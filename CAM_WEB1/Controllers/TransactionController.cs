using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CAM_WEB1.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    // Only Officer and Manager roles are authorized at the API level
    [Authorize(Roles = "Officer,Manager")]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to extract UserID from JWT Claims
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        [HttpPost("InitiateTransaction")]
        public async Task<ActionResult<Transaction>> CreateTransaction([FromBody] Transaction transaction)
        {
            if (transaction == null) return BadRequest("Invalid payload.");
            if (transaction.Amount <= 0) return BadRequest("Amount must be greater than zero.");

            // Extract the UserID from the logged-in user's token
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("User ID not found in token.");

            var transactionIdParam = new SqlParameter("@TransactionID", SqlDbType.Int) { Direction = ParameterDirection.Output };

            var parameters = new[]
            {
                new SqlParameter("@Action", SqlDbType.NVarChar, 50) { Value = "CREATE" },
                new SqlParameter("@AccountID", SqlDbType.Int) { Value = transaction.AccountID },
                new SqlParameter("@Type", SqlDbType.NVarChar, 20) { Value = transaction.Type ?? (object)DBNull.Value },
                new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = transaction.Amount },
                new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = (object)DBNull.Value }, // Let SP handle default status
                new SqlParameter("@Date", SqlDbType.DateTime2) { Value = transaction.Date == default ? DateTime.UtcNow : transaction.Date },
                new SqlParameter("@DateFrom", SqlDbType.DateTime2) { Value = DBNull.Value },
                new SqlParameter("@DateTo", SqlDbType.DateTime2) { Value = DBNull.Value },
                new SqlParameter("@PerformedByUserID", SqlDbType.Int) { Value = currentUserId }, // Passed to SP for validation
                transactionIdParam
            };

            try
            {
                var sql = "EXEC dbo.usp_Transaction @Action, @AccountID, @Type, @Amount, @Status, @Date, @DateFrom, @DateTo, @PerformedByUserID, @TransactionID OUTPUT";
                await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                var createdId = (int)(transactionIdParam.Value ?? 0);
                if (createdId == 0) return BadRequest("Failed to create transaction.");

                // If high value, the SP sets status to 'PendingApproval'. 
                // We create the Approval record in C# if needed.
                if (transaction.Amount > 100000m)
                {
                    var reviewer = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "Manager" || u.Role == "Officer");

                    if (reviewer != null)
                    {
                        var approval = new Approval
                        {
                            TransactionID = createdId,
                            ReviewerID = reviewer.UserID,
                            Decision = "Pending",
                            Comments = $"High-value transaction: {transaction.Amount}",
                            ApprovalDate = DateTime.UtcNow
                        };
                        _context.Approvals.Add(approval);
                        await _context.SaveChangesAsync();
                    }
                }

                // Return the created transaction details
                var createdList = await _context.Transactions
                    .FromSqlRaw("EXEC dbo.usp_Transaction @Action = 'GETBYID', @TransactionID = @id", new SqlParameter("@id", createdId))
                    .AsNoTracking()
                    .ToListAsync();

                return CreatedAtAction(nameof(GetTransactionById), new { id = createdId }, createdList.FirstOrDefault());
            }
            catch (SqlException ex)
            {
                // The SP RAISERROR messages will be caught here
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("TransactionHistory")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions(
            [FromQuery] int? accountId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var parameters = new[]
            {
                new SqlParameter("@Action", "GETALL"),
                new SqlParameter("@AccountID", accountId ?? (object)DBNull.Value),
                new SqlParameter("@Type", type ?? (object)DBNull.Value),
                new SqlParameter("@Status", status ?? (object)DBNull.Value),
                new SqlParameter("@DateFrom", dateFrom ?? (object)DBNull.Value),
                new SqlParameter("@DateTo", dateTo ?? (object)DBNull.Value)
            };

            var transactions = await _context.Transactions
                .FromSqlRaw("EXEC dbo.usp_Transaction @Action=@Action, @AccountID=@AccountID, @Type=@Type, @Status=@Status, @DateFrom=@DateFrom, @DateTo=@DateTo", parameters)
                .AsNoTracking()
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("ViewTransactiondetails/{id:int}")]
        public async Task<ActionResult<Transaction>> GetTransactionById(int id)
        {
            var list = await _context.Transactions
                .FromSqlRaw("EXEC dbo.usp_Transaction @Action = 'GETBYID', @TransactionID = @id", new SqlParameter("@id", id))
                .AsNoTracking()
                .ToListAsync();

            var txn = list.FirstOrDefault();
            return txn == null ? NotFound() : txn;
        }

        [HttpPatch("{id:int}/ChangePaymentstatus")]
        [Authorize(Roles = "Manager")] // Restrict status changes to Managers only
        public async Task<IActionResult> ChangeTransactionStatus(int id, [FromBody] string newStatus)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_Transaction @Action = 'UPDATESTATUS', @TransactionID = @id, @Status = @s",
                new SqlParameter("@id", id),
                new SqlParameter("@s", newStatus)
            );
            return Ok(new { message = "Status updated" });
        }

        [HttpDelete("{id:int}/CancelTransaction")]
        [Authorize(Roles = "Manager")] // Restrict deletion to Managers only
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_Transaction @Action = 'DELETE', @TransactionID = @id",
                new SqlParameter("@id", id)
            );
            return NoContent();
        }
    }
}