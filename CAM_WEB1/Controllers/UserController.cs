using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Microsoft.Data.SqlClient;

using CAM_WEB1.Data;

using CAM_WEB1.Models;

namespace CAM_WEB1.Controllers

{

    [Route("api/[controller]")]

    [ApiController]

    public class UsersController : ControllerBase

    {

        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)

        {

            _context = context;

        }

        // ============================

        // CREATE USER

        // POST: api/Users

        // ============================

        [HttpPost]

        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)

        {

            await _context.Database.ExecuteSqlRawAsync(

                "EXEC dbo.sp_User_Create @Name, @Email, @Role, @Branch",

                new SqlParameter("@Name", dto.Name),

                new SqlParameter("@Email", dto.Email),

                new SqlParameter("@Role", dto.Role),

                new SqlParameter("@Branch", dto.Branch ?? (object)DBNull.Value)

            );

            return Ok(new { message = "User created successfully" });

        }

        // ============================

        // GET USERS (FILTER)

        // GET: api/Users?role=&branch=&status=

        // ============================

        [HttpGet]

        public async Task<IActionResult> GetUsers(

            [FromQuery] string? role,

            [FromQuery] string? branch,

            [FromQuery] string? status)

        {

            var users = await _context.Users

                .FromSqlRaw(

                    "EXEC dbo.sp_User_GetAll @Role, @Branch, @Status",

                    new SqlParameter("@Role", (object?)role ?? DBNull.Value),

                    new SqlParameter("@Branch", (object?)branch ?? DBNull.Value),

                    new SqlParameter("@Status", (object?)status ?? DBNull.Value)

                )

                .ToListAsync();

            return Ok(users);

        }

        // ============================

        // GET USER BY ID  ✅ FIXED

        // GET: api/Users/5

        // ============================

        [HttpGet("{id}")]

        public async Task<IActionResult> GetUserById(int id)

        {

            var result = await _context.Users

                .FromSqlRaw(

                    "EXEC dbo.sp_User_GetById @UserID",

                    new SqlParameter("@UserID", id)

                )

                .ToListAsync();   // ✅ IMPORTANT

            var user = result.FirstOrDefault();

            if (user == null)

                return NotFound(new { message = "User not found" });

            return Ok(user);

        }

        // ============================

        // UPDATE USER

        // PUT: api/Users/5

        // ============================

        [HttpPut("{id}")]

        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)

        {

            if (id != user.UserID)

                return BadRequest(new { message = "ID mismatch" });

            await _context.Database.ExecuteSqlRawAsync(

                "EXEC dbo.sp_User_Update @UserID, @Name, @Email, @Role, @Branch, @Status",

                new SqlParameter("@UserID", id),

                new SqlParameter("@Name", user.Name),

                new SqlParameter("@Email", user.Email),

                new SqlParameter("@Role", user.Role),

                new SqlParameter("@Branch", user.Branch ?? (object)DBNull.Value),

                new SqlParameter("@Status", user.Status)

            );

            return Ok(new { message = "User updated successfully" });

        }

        // ============================

        // UPDATE STATUS ONLY

        // PATCH: api/Users/5/status

        // ============================

        [HttpPatch("{id}/status")]

        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UserStatusDto dto)

        {

            await _context.Database.ExecuteSqlRawAsync(

                "EXEC dbo.sp_User_UpdateStatus @UserID, @Status",

                new SqlParameter("@UserID", id),

                new SqlParameter("@Status", dto.Status)

            );

            return Ok(new { message = "User status updated successfully" });

        }

        // ============================

        // DELETE USER

        // DELETE: api/Users/5

        // ============================

        [HttpDelete("{id}")]

        public async Task<IActionResult> DeleteUser(int id)

        {

            await _context.Database.ExecuteSqlRawAsync(

                "EXEC dbo.sp_User_Delete @UserID",

                new SqlParameter("@UserID", id)

            );

            return Ok(new { message = "User deleted successfully" });

        }

    }

    // ============================

    // DTOs

    // ============================

    public class CreateUserDto

    {

        public string Name { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public string? Branch { get; set; }

    }

    public class UserStatusDto

    {

        public string Status { get; set; } // Active / Inactive

    }

}
