using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CAM_WEB1.Data;
using CAM_WEB1.Models;
using System.Text.Json;

namespace CAM_WEB1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/reports/generate
        // Logic: Scans Transaction and Account tables to save a snapshot report
        [HttpPost("generate")]
        public async Task<ActionResult<Report>> GenerateReport([FromQuery] string scope = "Global Summary")
        {
            // 1. Transaction Volume Analysis (Module 2.3)
            var totalTxns = await _context.Transactions.CountAsync();
            var totalVolume = await _context.Transactions.SumAsync(t => t.Amount);

            // 2. High-Value Count (Module 4.5)
            // LLD identifies "High-Value" for Manager review. We use > 10,000.
            var highValueCount = await _context.Transactions
                .Where(t => t.Amount > 10000)
                .CountAsync();

            // 3. Account Growth Trends (Module 2.2)
            var totalAccounts = await _context.Accounts.CountAsync();
            var breakdown = await _context.Accounts
                .GroupBy(a => a.AccountType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            // 4. Formatting the Metrics into JSON for the Report table
            var metricsObj = new
            {
                TotalTransactions = totalTxns,
                TotalVolume = totalVolume,
                HighValueTransactions = highValueCount,
                TotalAccounts = totalAccounts,
                AccountTypeBreakdown = breakdown
            };

            var report = new Report
            {
                Scope = scope,
                Metrics = JsonSerializer.Serialize(metricsObj),
                GeneratedDate = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            // Audit Log (Following team pattern)
            _context.Set<ReportAudit>().Add(new ReportAudit { ReportId = report.ReportId });

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReportById), new { id = report.ReportId }, report);
        }

        // GET: api/reports/dashboard
        // Logic: Provides live real-time data for the Admin/Manager Dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetLiveDashboard()
        {
            var data = new
            {
                SystemOverview = new
                {
                    ActiveUsers = await _context.Users.CountAsync(u => u.Status == "Active"),
                    TotalAccounts = await _context.Accounts.CountAsync(),
                    SystemTotalBalance = await _context.Accounts.SumAsync(a => a.Balance)
                },
                Compliance = new
                {
                    PendingApprovals = await _context.Approvals.CountAsync(a => a.Decision == "Pending"),
                    RecentHighValueAlerts = await _context.Transactions
                        .Where(t => t.Amount > 10000)
                        .OrderByDescending(t => t.Date)
                        .Take(5)
                        .ToListAsync()
                }
            };

            return Ok(data);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Report>>> GetHistory()
        {
            return await _context.Reports.OrderByDescending(r => r.GeneratedDate).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Report>> GetReportById(long id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();
            return report;
        }
    }
}