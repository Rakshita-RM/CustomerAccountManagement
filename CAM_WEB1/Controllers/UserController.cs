using CAM_WEB1.Data;
using CAM_WEB1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CAM_WEB1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "Officer", "Manager", "Admin" };

        private static readonly HashSet<string> AllowedStatus =
            new(StringComparer.OrdinalIgnoreCase) { "Active", "Inactive" };

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (user == null) return BadRequest("Invalid payload.");

            // Normalize & validate
            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "Officer" : user.Role.Trim();
            user.Status = string.IsNullOrWhiteSpace(user.Status) ? "Active" : user.Status.Trim();

            if (!AllowedRoles.Contains(user.Role))
                return BadRequest("Role must be one of: Officer, Manager, Admin.");
            if (!AllowedStatus.Contains(user.Status))
                return BadRequest("Status must be one of: Active, Inactive.");

            // Optional: prevent duplicate emails
            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists) return Conflict("A user with the same email already exists.");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = user.UserID }, user);
        }

        // GET: api/users
        // Filters: role, status, branch
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(
            [FromQuery] string? role,
            [FromQuery] string? status,
            [FromQuery] string? branch)
        {
            var q = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(u => u.Status == status);

            if (!string.IsNullOrWhiteSpace(branch))
                q = q.Where(u => u.Branch == branch);

            var items = await q.OrderBy(u => u.Name).ToListAsync();
            return Ok(items);
        }

        // GET: api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return user;
        }

        // PUT: api/users/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            if (id != user.UserID) return BadRequest("ID mismatch.");

            // Validate fields
            if (!string.IsNullOrWhiteSpace(user.Role) && !AllowedRoles.Contains(user.Role))
                return BadRequest("Role must be one of: Officer, Manager, Admin.");

            if (!string.IsNullOrWhiteSpace(user.Status) && !AllowedStatus.Contains(user.Status))
                return BadRequest("Status must be one of: Active, Inactive.");

            // Enforce unique email (excluding this user)
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == user.Email && u.UserID != id);
            if (emailExists) return Conflict("A user with the same email already exists.");

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Users.AnyAsync(u => u.UserID == id);
                if (!exists) return NotFound();
                throw;
            }

            return NoContent();
        }

        // PATCH: api/users/{id}/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
                return BadRequest("New status is required.");

            var status = newStatus.Trim();
            if (!AllowedStatus.Contains(status))
                return BadRequest("Status must be one of: Active, Inactive.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"User status updated to {status}" });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}