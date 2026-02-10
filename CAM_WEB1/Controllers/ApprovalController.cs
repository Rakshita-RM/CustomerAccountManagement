using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CAM_WEB1.Controllers
{
    [ApiController]
    [Route("api/v1/approvals")]
    [Authorize(Roles = "Manager")]   // ✅ ONLY MANAGER ACCESS
    public class ApprovalController : ControllerBase
    {
        private readonly string _conn;

        private readonly ApplicationDbContext _context;

        public ApprovalController(ApplicationDbContext context, IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("DefaultConnection") ?? "";
            _context = context;
        }

        // =========================
        // GET ALL APPROVALS
        // =========================
        [HttpGet]
        public IActionResult GetAll()
        {
            var approvals = _context.Approvals
                                    .OrderByDescending(a => a.ApprovalDate)
                                    .ToList();

            return Ok(approvals);
        }

        // =========================
        // GET APPROVAL BY ID
        // =========================
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var approval = _context.Approvals
                                   .FirstOrDefault(a => a.ApprovalID == id);

            if (approval == null)
                return NotFound();

            return Ok(approval);
        }

        // =========================
        // APPROVE / REJECT (POST)
        // =========================
        [HttpPost("{id}/decision")]
        public IActionResult SubmitDecision(
            int id,
            [FromBody] Approval request)
        {
            var parameters = new[]
            {
                new SqlParameter("@Action", "Update"),
                new SqlParameter("@ApprovalId", id),
                new SqlParameter("@ReviewerId", request.ReviewerID), // Manager
                new SqlParameter("@Decision", request.Decision),
                new SqlParameter("@Comments", request.Comments ?? "")
            };

            _context.Database.ExecuteSqlRaw(
                "EXEC usp_Approval @Action, @ApprovalId, NULL, @ReviewerId, @Decision, @Comments",
                parameters
            );
            

            Audit(request.ReviewerID, request.Comments, null,request.Decision );

            return Ok("Decision submitted successfully");
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