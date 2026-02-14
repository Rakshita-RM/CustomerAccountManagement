using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Microsoft.Data.SqlClient;

using CAM_WEB1.Data;

using CAM_WEB1.Models;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

using System.Text;

namespace CAM_WEB1.Controllers

{

    [ApiController]

    [Route("api/v1/reports")]

    [Authorize(Roles = "Manager,Admin")]

    public class ReportsController : ControllerBase

    {

        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)

        {

            _context = context;

        }

        [HttpPost("generate")]

        public async Task<IActionResult> GenerateSnapshot([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string branch = "Global")

        {

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int userId = int.TryParse(userIdStr, out int id) ? id : 0;

            var pMode = new SqlParameter("@Mode", "GENERATE");

            var pFrom = new SqlParameter("@FromDate", from);

            var pTo = new SqlParameter("@ToDate", to);

            var pBranch = new SqlParameter("@BranchName", branch);

            await _context.Database.ExecuteSqlRawAsync(

                "EXEC usp_Reports_Master @Mode, @BranchName, @FromDate, @ToDate",

                pMode, pBranch, pFrom, pTo);

            var latest = await _context.Reports

                .OrderByDescending(r => r.ReportID)

                .FirstOrDefaultAsync();

            if (latest != null)

            {

                _context.ReportAudits.Add(new ReportAudit

                {

                    ReportID = latest.ReportID,

                    UserID = userId,

                    Action = "GENERATE",

                    ActionDate = DateTime.UtcNow

                });

                await _context.SaveChangesAsync();

                // This will now automatically show "branch" if you renamed it in your Models/Report.cs

                return Ok(latest);

            }

            return BadRequest(new { message = "Procedure executed but no report was generated." });

        }

        [HttpGet("list")]

        public async Task<IActionResult> GetReportList()

        {

            var reports = await _context.Reports.OrderByDescending(r => r.GeneratedDate).ToListAsync();

            return Ok(reports);

        }

        // --- API 3: EXPORT TO CSV ---

        [HttpGet("{id}/export")]

        public async Task<IActionResult> ExportReport(int id)

        {

            var report = await _context.Reports.FindAsync(id);

            if (report == null) return NotFound();

            var csv = new StringBuilder();

            csv.AppendLine("ReportID,Branch,TotalTransactions,HighValueCount,AccountGrowthRate,GeneratedDate");



            csv.AppendLine($"{report.ReportID},{report.Branch},{report.TotalTransactions},{report.HighValueCount},{report.AccountGrowthRate}%,{report.GeneratedDate}");

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Report_{id}.csv");

        }

        [HttpGet("system-audits")]

        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> GetSystemAudits()

        {

            var audits = await _context.ReportAudits

                .OrderByDescending(a => a.ActionDate)

                .Take(100)

                .ToListAsync();

            return Ok(audits);

        }

        [HttpDelete("{id}")]

        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteReport(int id)

        {

            var report = await _context.Reports.FindAsync(id);

            if (report == null) return NotFound(new { message = "Report not found" });

            var audits = _context.ReportAudits.Where(a => a.ReportID == id);

            _context.ReportAudits.RemoveRange(audits);

            _context.Reports.Remove(report);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Report {id} deleted successfully" });

        }

    }

}
