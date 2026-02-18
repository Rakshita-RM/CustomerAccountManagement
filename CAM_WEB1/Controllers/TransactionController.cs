using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CAM_WEB1.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize(Roles = "Officer,Manager")]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _conn;
        private readonly IConfiguration _config;
        public TransactionsController(ApplicationDbContext context,IConfiguration configuration)
        {
            _context = context;
            _conn = configuration.GetConnectionString("DefaultConnection");
        }

        // ==========================================================
        // CREATE TRANSACTION (Deposit / Withdraw / Transfer)
        // Officer Only
        // ==========================================================
        [HttpPost("initiate")]
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> CreateTransaction([FromBody] Transaction transaction)
        {
            if (transaction == null)
                return BadRequest("Invalid request payload");

            try 
            {
                var parameters = new[]
                {
                    new SqlParameter("@Action", "CREATE"),
                    new SqlParameter("@AccountID", transaction.AccountID),
                    new SqlParameter("@ToAccountID",
                        (object?)transaction.ToAccountID ?? DBNull.Value),
                    new SqlParameter("@Type", transaction.Type),
                    new SqlParameter("@Amount", transaction.Amount)
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC usp_Transaction @Action,@AccountID,@ToAccountID,@Type,@Amount",
                    parameters);

                Audit(transaction.TransactionID, "CREATE", null, $"Type: {transaction.Type}, Amount: {transaction.Amount}");
                return Ok(new
                {
                    message = "Transaction processed successfully"
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    message = "Database error occurred",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        // ==========================================================
        // GET ALL TRANSACTIONS
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _context.Transactions
                    .FromSqlRaw("EXEC usp_Transaction @Action='GETALL'")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving transactions",
                    error = ex.Message
                });
            }
        }

        // ==========================================================
        // GET TRANSACTION BY ID
        // ==========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var list = await _context.Transactions
                    .FromSqlRaw(
                        "EXEC usp_Transaction @Action='GETBYID', @TransactionID=@id",
                        new SqlParameter("@id", id))
                    .AsNoTracking()
                    .ToListAsync();

                var transaction = list.FirstOrDefault();

                if (transaction == null)
                    return NotFound("Transaction not found");

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving transaction",
                    error = ex.Message
                });
            }
        }
        private void Audit(int UserID, string action, string oldVal, string newVal)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = new SqlCommand("usp_user_audit", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserID", UserID);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@OldValue", (object?)oldVal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewValue", (object?)newVal ?? DBNull.Value);

            con.Open();
            cmd.ExecuteNonQuery();
        }

    }
}
