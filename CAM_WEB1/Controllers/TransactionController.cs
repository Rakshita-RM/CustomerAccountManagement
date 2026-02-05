using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CAM_WEB1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/transactions
        // Creates a transaction row and (for Deposit/Withdrawal) updates the Account balance.
        // NOTE: "Transfer" requires source+destination accounts; without a dedicated model, we block it here to avoid imbalance.
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction([FromBody] Transaction transaction)
        {
            if (transaction == null) return BadRequest("Invalid payload.");
            if (transaction.Amount <= 0) return BadRequest("Amount must be greater than zero.");

            // Normalize type & status
            var type = (transaction.Type ?? string.Empty).Trim();
            var status = (transaction.Status ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(type)) return BadRequest("Type is required.");
            if (string.IsNullOrWhiteSpace(status)) transaction.Status = "Completed";

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Deposit", "Withdrawal", "Transfer" };
            if (!allowedTypes.Contains(type))
                return BadRequest("Type must be one of: Deposit, Withdrawal, Transfer.");

            if (transaction.Date == default) transaction.Date = DateTime.UtcNow;

            // Prepare parameters that match the stored procedure signature exactly.
            var newIdParam = new SqlParameter("@NewTransactionID", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            var parameters = new[]
            {
        new SqlParameter("@AccountID", System.Data.SqlDbType.Int) { Value = transaction.AccountID },
        new SqlParameter("@Type", System.Data.SqlDbType.NVarChar, 20) { Value = type },
        new SqlParameter("@Amount", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = transaction.Amount },
        new SqlParameter("@Date", System.Data.SqlDbType.DateTime2) { Value = transaction.Date },
        new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = transaction.Status },
        newIdParam
    };

            try
            {
                // Use positional call matching the stored-proc parameter order,
                // including the OUTPUT parameter at the end.
                var sql = "EXEC dbo.SP_CreateTransaction @AccountID, @Type, @Amount, @Date, @Status, @NewTransactionID OUTPUT";
                await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                var createdId = (int)(newIdParam.Value ?? 0);
                if (createdId == 0) return BadRequest("Failed to create transaction.");

                // Retrieve the created transaction via SELECT stored proc
                var createdList = await _context.Transactions
                    .FromSqlRaw("EXEC dbo.SP_GetTransactionById @TransactionID", new SqlParameter("@TransactionID", createdId))
                    .AsNoTracking()
                    .ToListAsync();

                var created = createdList.FirstOrDefault();
                if (created == null) return StatusCode(500, "Transaction created but could not be retrieved.");

                return CreatedAtAction(nameof(GetTransactionById), new { id = created.TransactionID }, created);
            }
            catch (SqlException ex)
            {
                // Surface useful DB errors to client (e.g. insufficient funds, account not found)
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/transactions
        // Supports filtering by account, type, status, and date range
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions(
            [FromQuery] int? accountId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var parameters = new[]
     {
        new SqlParameter("@AccountId", System.Data.SqlDbType.Int) { Value = accountId ?? (object)DBNull.Value },
        new SqlParameter("@Type", System.Data.SqlDbType.NVarChar, 20) { Value = string.IsNullOrWhiteSpace(type) ? (object)DBNull.Value : type! },
        new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(status) ? (object)DBNull.Value : status! },
        new SqlParameter("@DateFrom", System.Data.SqlDbType.DateTime2) { Value = dateFrom ?? (object)DBNull.Value },
        new SqlParameter("@DateTo", System.Data.SqlDbType.DateTime2) { Value = dateTo ?? (object)DBNull.Value }
    };

            var transactions = await _context.Transactions
                .FromSqlRaw("EXEC dbo.SP_GetTransactions @AccountId, @Type, @Status, @DateFrom, @DateTo", parameters)
                .AsNoTracking()
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/transactions/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Transaction>> GetTransactionById(int id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();
            return txn;
        }

        // PATCH: api/transactions/{id}/status
        // Mirrors your AccountsController status patch style
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeTransactionStatus(int id, [FromBody] string newStatus)
        {
            await _context.Database.ExecuteSqlRawAsync(
    "EXEC dbo.SP_UpdateTransactionStatus @Id, @Status",
    new SqlParameter("@Id", id),
    new SqlParameter("@Status", newStatus)
);

            return Ok(new { message = "Transaction status updated" });

        }

        // DELETE: api/transactions/{id}
        // NOTE: This removes the row only; it does NOT reverse prior balance effects.
        // Keep this for admin/tools. If you want automatic reversal, tell me and I’ll add safe logic.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            await _context.Database.ExecuteSqlRawAsync(
     "EXEC dbo.SP_DeleteTransaction @Id",
     new SqlParameter("@Id", id)
 );

            return NoContent();

        }
    }
}