using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CAM_WEB1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedDecisions =
            new(StringComparer.OrdinalIgnoreCase) { "Pending", "Approve", "Reject" };

        private readonly ApplicationDbContext _approvalContext;
        private readonly ApplicationDbContext _appContext; // for cross-entity validation

        public ApprovalsController(ApplicationDbContext approvalContext, ApplicationDbContext appContext)
        {
            _approvalContext = approvalContext;
            _appContext = appContext;
        }

        // POST: api/approvals
        [HttpPost]
        public async Task<ActionResult<Approval>> CreateApproval([FromBody] Approval approval)
        {
            if (approval == null) return BadRequest("Invalid payload.");

            // Normalize & validate
            approval.Decision = string.IsNullOrWhiteSpace(approval.Decision) ? "Pending" : approval.Decision.Trim();
            if (!AllowedDecisions.Contains(approval.Decision))
                return BadRequest("Decision must be one of: Pending, Approve, Reject.");

            if (approval.ApprovalDate == default) approval.ApprovalDate = DateTime.UtcNow;

            // Optional referential checks using your existing ApplicationDbContext
            var txnExists = await _appContext.Transactions.AnyAsync(t => t.TransactionID == approval.TransactionID);
            if (!txnExists) return NotFound($"Transaction {approval.TransactionID} not found.");

            var reviewerExists = await _appContext.Users.AnyAsync(u => u.UserID == approval.ReviewerID);
            if (!reviewerExists) return NotFound($"Reviewer (UserID={approval.ReviewerID}) not found.");

            _approvalContext.Approvals.Add(approval);
            await _approvalContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApprovalById), new { id = approval.ApprovalID }, approval);
        }

        // GET: api/approvals
        // Filters: transactionId, reviewerId, decision, from, to
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Approval>>> GetApprovals(
            [FromQuery] int? transactionId,
            [FromQuery] int? reviewerId,
            [FromQuery] string? decision,
            [FromQuery(Name = "from")] DateTime? fromDate,
            [FromQuery(Name = "to")] DateTime? toDate)
        {
            var q = _approvalContext.Approvals.AsQueryable();

            if (transactionId.HasValue)
                q = q.Where(a => a.TransactionID == transactionId.Value);

            if (reviewerId.HasValue)
                q = q.Where(a => a.ReviewerID == reviewerId.Value);

            if (!string.IsNullOrWhiteSpace(decision))
                q = q.Where(a => a.Decision == decision);

            if (fromDate.HasValue)
                q = q.Where(a => a.ApprovalDate >= fromDate.Value);

            if (toDate.HasValue)
                q = q.Where(a => a.ApprovalDate <= toDate.Value);

            var items = await q.OrderByDescending(a => a.ApprovalDate).ToListAsync();
            return Ok(items);
        }

        // GET: api/approvals/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Approval>> GetApprovalById(int id)
        {
            var approval = await _approvalContext.Approvals.FindAsync(id);
            if (approval == null) return NotFound();
            return approval;
        }

        // PUT: api/approvals/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateApproval(int id, [FromBody] Approval approval)
        {
            if (id != approval.ApprovalID) return BadRequest("ID mismatch.");

            // Validate decision
            if (string.IsNullOrWhiteSpace(approval.Decision) || !AllowedDecisions.Contains(approval.Decision.Trim()))
                return BadRequest("Decision must be one of: Pending, Approve, Reject.");

            _approvalContext.Entry(approval).State = EntityState.Modified;

            try
            {
                await _approvalContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _approvalContext.Approvals.AnyAsync(a => a.ApprovalID == id);
                if (!exists) return NotFound();
                throw;
            }

            return NoContent();
        }

        // PATCH: api/approvals/{id}/decision
        [HttpPatch("{id:int}/decision")]
        public async Task<IActionResult> ChangeDecision(int id, [FromBody] string newDecision)
        {
            if (string.IsNullOrWhiteSpace(newDecision))
                return BadRequest("New decision is required.");

            var decision = newDecision.Trim();
            if (!AllowedDecisions.Contains(decision))
                return BadRequest("Decision must be one of: Pending, Approve, Reject.");

            var approval = await _approvalContext.Approvals.FindAsync(id);
            if (approval == null) return NotFound();

            approval.Decision = decision;
            if (approval.ApprovalDate == default) approval.ApprovalDate = DateTime.UtcNow;

            await _approvalContext.SaveChangesAsync();
            return Ok(new { Message = $"Decision updated to {decision}" });
        }

        // DELETE: api/approvals/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteApproval(int id)
        {
            var approval = await _approvalContext.Approvals.FindAsync(id);
            if (approval == null) return NotFound();

            _approvalContext.Approvals.Remove(approval);
            await _approvalContext.SaveChangesAsync();

            return NoContent();
        }
    }
}