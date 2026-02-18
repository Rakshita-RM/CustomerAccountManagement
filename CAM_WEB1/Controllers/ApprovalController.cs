using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace CAM_WEB1.Controllers
{
    [ApiController]
    [Route("api/approvals")]
    [Authorize(Roles = "Manager")]
    public class ApprovalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _conn;
        public ApprovalController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _conn = configuration.GetConnectionString("DefaultConnection");
        }

        // ==========================================================
        // GET ALL APPROVALS
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var approvals = await _context.Approvals
                    .FromSqlRaw("EXEC usp_Approval @Action='GetAll'")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(approvals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving approvals",
                    error = ex.Message
                });
            }
        }

        // ==========================================================
        // GET APPROVAL BY ID
        // ==========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var approvals = await _context.Approvals
                    .FromSqlRaw(
                        "EXEC usp_Approval @Action='GetById', @ApprovalId=@id",
                        new SqlParameter("@id", id))
                    .AsNoTracking()
                    .ToListAsync();

                var approval = approvals.FirstOrDefault();

                if (approval == null)
                    return NotFound("Approval not found");

                return Ok(approval);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving approval",
                    error = ex.Message
                });
            }
        }

        // ==========================================================
        // APPROVE / REJECT TRANSACTION
        // ReviewerID Automatically Taken From JWT
        // ==========================================================
        [HttpPost("{id}/decision")]
        public async Task<IActionResult> SubmitDecision(
            int id,
            [FromBody] Approval request)
        {
            if (request == null || string.IsNullOrEmpty(request.Decision))
                return BadRequest("Invalid decision request");

            try
            {
                // 🔥 Automatically get Manager ID from token
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null)
                    return Unauthorized("Invalid token");

                int reviewerId = int.Parse(claim.Value);

                var parameters = new[]
                {
                    new SqlParameter("@Action", "Update"),
                    new SqlParameter("@ApprovalId", id),
                    new SqlParameter("@ReviewerId", reviewerId),
                    new SqlParameter("@Decision", request.Decision),
                    new SqlParameter("@Comments", request.Comments ?? "") 
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC usp_Approval @Action, @ApprovalId, NULL, @ReviewerId, @Decision, @Comments",
                    parameters);

                Audit(request.ReviewerID, $"Approval {request.Decision}", $"ApprovalID: {id}", $" Comments: {request.Comments}");
                return Ok(new
                {
                    message = "Decision submitted successfully"
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
