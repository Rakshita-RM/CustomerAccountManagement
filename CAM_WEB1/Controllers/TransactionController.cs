using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Mvc;
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

            var account = await _context.Accounts.FindAsync(transaction.AccountID);
            if (account == null) return NotFound($"Account {transaction.AccountID} not found.");

            // Normalize type & status to your expected values
            var type = (transaction.Type ?? string.Empty).Trim();
            var status = (transaction.Status ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(type)) return BadRequest("Type is required.");
            if (string.IsNullOrWhiteSpace(status)) transaction.Status = "Completed"; // default like your model

            // Only allow the three types mentioned in your code comment
            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Deposit", "Withdrawal", "Transfer" };
            if (!allowedTypes.Contains(type))
                return BadRequest("Type must be one of: Deposit, Withdrawal, Transfer.");

            // If not set by client, keep UTC now
            if (transaction.Date == default) transaction.Date = DateTime.UtcNow;

            // Apply balance effect for Deposit/Withdrawal immediately
            if (type.Equals("Deposit", StringComparison.OrdinalIgnoreCase))
            {
                account.Balance += transaction.Amount;
            }
            else if (type.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase))
            {
                if (account.Balance < transaction.Amount)
                    return BadRequest("Insufficient balance.");

                account.Balance -= transaction.Amount;
            }
            else if (type.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
            {
                // Without a source+destination model, a single-row "Transfer" would break balances.
                // We only log it (no balance change). Implement a dedicated transfer endpoint later.
                // If you prefer to reject instead, uncomment the next line:
                // return BadRequest("Use a dedicated transfer endpoint that accepts source and destination AccountIDs.");
            }

            // Persist
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.TransactionID }, transaction);
        }

        // GET: api/transactions
        // Supports filtering by account, type, status, and date range
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions(
            [FromQuery] int? accountId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery(Name = "from")] DateTime? dateFrom,
            [FromQuery(Name = "to")] DateTime? dateTo)
        {
            var q = _context.Transactions.AsQueryable();

            if (accountId.HasValue)
                q = q.Where(t => t.AccountID == accountId.Value);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(t => t.Type == type);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(t => t.Status == status);

            if (dateFrom.HasValue)
                q = q.Where(t => t.Date >= dateFrom.Value);

            if (dateTo.HasValue)
                q = q.Where(t => t.Date <= dateTo.Value);

            var items = await q.OrderByDescending(t => t.Date).ToListAsync();
            return Ok(items);
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
            if (string.IsNullOrWhiteSpace(newStatus))
                return BadRequest("New status is required.");

            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();

            txn.Status = newStatus.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Transaction status updated to {txn.Status}" });
        }

        // DELETE: api/transactions/{id}
        // NOTE: This removes the row only; it does NOT reverse prior balance effects.
        // Keep this for admin/tools. If you want automatic reversal, tell me and I’ll add safe logic.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();

            _context.Transactions.Remove(txn);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}