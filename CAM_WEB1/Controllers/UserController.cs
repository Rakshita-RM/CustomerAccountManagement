using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CAM_WEB1.DTO;
using CAM_WEB1.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace CAM_WEB1.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsersController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly string _conn;

		public UsersController(IConfiguration configuration)
		{
			_config = configuration;
			_conn = configuration.GetConnectionString("DefaultConnection");
		}

		private List<Dictionary<string, object>> ToList(DataTable table)
		{
			var list = new List<Dictionary<string, object>>();

			foreach (DataRow row in table.Rows)
			{
				var dict = new Dictionary<string, object>();
				foreach (DataColumn col in table.Columns)
				{
					dict[col.ColumnName] = row[col];
				}
				list.Add(dict);
			}

			return list;
		}

        // =========================
        // 1. FIRST ADMIN (ONE TIME)
        // =========================
        [HttpPost("first-admin")]
        [AllowAnonymous]
        public IActionResult FirstAdmin(UserCreateRequest req)
        {
            try
            {
                using var con = new SqlConnection(_conn);
                using var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM t_User WHERE Role = 'ADMIN'", con);

                con.Open();

                int adminCount = (int)checkCmd.ExecuteScalar();

                if (adminCount > 0)
                {
                    return Conflict(new
                    {
                        message = "First admin already exists"
                    });
                }

                // If not exists → Create admin
                using var cmd = new SqlCommand("usp_user_crud", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "FIRST_ADMIN");
                cmd.Parameters.AddWithValue("@Name", req.Name);
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.Parameters.AddWithValue("@PasswordHash",
                    BCrypt.Net.BCrypt.HashPassword(req.Password));
                cmd.Parameters.AddWithValue("@Branch", req.Branch);
                cmd.Parameters.AddWithValue("@Status", "Active");

                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    message = "First admin created successfully"
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




        //GET CURRENT USER PROFILE

        [HttpGet("me")]
		[Authorize(Roles = "Officer,Manager,Admin")] // Use "Officer" here to match normalized JWT
		public IActionResult GetProfile()
		{
			int currentId = GetUserID();

			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "GET");
			cmd.Parameters.AddWithValue("@UserID", currentId);

			con.Open();
			DataTable dt = new();
			dt.Load(cmd.ExecuteReader());

			var user = ToList(dt).FirstOrDefault();
			if (user == null) return NotFound("User profile not found.");

			return Ok(user);
		}



		// =========================
		// 2. LOGIN
		// =========================
		[HttpPost("login")]
		[AllowAnonymous]
		public IActionResult Login(LoginRequest1 req)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "LOGIN");
			cmd.Parameters.AddWithValue("@Email", req.Email);

			con.Open();
			using var rd = cmd.ExecuteReader();

			if (!rd.Read())
				return Unauthorized("Invalid credentials");

			string dbHash = rd["PasswordHash"].ToString();
			if (!BCrypt.Net.BCrypt.Verify(req.Password, dbHash))
				return Unauthorized("Invalid credentials");

			int UserID = (int)rd["UserID"];
			string role = rd["Role"].ToString();

			string accessToken = GenerateJwt(UserID, role);
			string refreshToken = Guid.NewGuid().ToString();

			rd.Close();

			// Save refresh token
			cmd.Parameters.Clear();
			cmd.Parameters.AddWithValue("@Action", "SAVE_REFRESH");
			cmd.Parameters.AddWithValue("@UserID", UserID);
			cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);
			cmd.ExecuteNonQuery();

			Audit(UserID, "LOGIN", null, "SUCCESS");

			return Ok(new
			{
				accessToken,
				refreshToken
			});
		}

		// =========================
		// 3. LOGOUT
		// =========================
		[HttpPost("logout")]
		[Authorize]
		public IActionResult Logout(RefreshTokenRequest req)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "LOGOUT");
			cmd.Parameters.AddWithValue("@RefreshToken", req.RefreshToken);

			con.Open();
			cmd.ExecuteNonQuery();

			Audit(GetUserID(), "LOGOUT", null, "SUCCESS");
			return Ok("Logged out");
		}

        // =========================
        // 4. CREATE USER (ADMIN)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(UserCreateRequest req)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = new SqlCommand("usp_user_crud", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "CREATE");
            cmd.Parameters.AddWithValue("@Name", req.Name);
            cmd.Parameters.AddWithValue("@Email", req.Email);
            cmd.Parameters.AddWithValue("@PasswordHash",
                BCrypt.Net.BCrypt.HashPassword(req.Password));
            cmd.Parameters.AddWithValue("@Role", req.Role);
            cmd.Parameters.AddWithValue("@Branch", req.Branch);

            con.Open();

            var result = cmd.ExecuteScalar()?.ToString();

            if (result == "EMAIL_EXISTS")
            {
                return BadRequest(new
                {
                    message = "Email already exists"
                });
            }

            Audit(GetUserID(), "CREATE_USER", null, req.Email);

            return Ok(new
            {
                message = "User created successfully"
            });
        }


        // =========================
        // 5. GET USERS (FILTERED)
        // =========================
        [HttpGet]
		[Authorize]
		public IActionResult Get(
			int? id,
			string? role,
			string? branch,
			string? status)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "GET");
			cmd.Parameters.AddWithValue("@UserID", (object?)id ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Role", (object?)role ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Branch", (object?)branch ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

			con.Open();
			DataTable dt = new();
			dt.Load(cmd.ExecuteReader());

			return Ok(ToList(dt));
		}

		// =========================
		// 6. UPDATE USER (PUT)
		// =========================
		[HttpPut("{id}")]
		[Authorize]
		public IActionResult Update(int id, UserUpdateRequest req)
		{
			int loggedUserID = GetUserID();
			bool isAdmin = User.IsInRole("Admin");

			if (!isAdmin && loggedUserID != id)
				return Forbid();

			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "UPDATE");
			cmd.Parameters.AddWithValue("@UserID", id);
			cmd.Parameters.AddWithValue("@Name", req.Name);
			cmd.Parameters.AddWithValue("@Branch", req.Branch);

			con.Open();
			cmd.ExecuteNonQuery();

			Audit(loggedUserID, "UPDATE_USER", null, $"UserID={id}");
			return Ok("Updated");
		}

		// =========================
		// 7. UPDATE STATUS (PATCH)
		// =========================
		[HttpPatch("{id}/status")]
		[Authorize(Roles = "Admin")]
		public IActionResult UpdateStatus(int id, string status)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_crud", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "STATUS");
			cmd.Parameters.AddWithValue("@UserID", id);
			cmd.Parameters.AddWithValue("@Status", status);

			con.Open();
			cmd.ExecuteNonQuery();

			Audit(GetUserID(), "UPDATE_STATUS", null, status);
			return Ok("Status updated");
		}

		// =========================
		// 8. DELETE USER
		//// =========================
		//[HttpDelete("{id}")]
		//[Authorize(Roles = "Admin")]
		//public IActionResult Delete(int id)
		//{
		//    using var con = new SqlConnection(_conn);
		//    using var cmd = new SqlCommand("usp_user_crud", con);
		//    cmd.CommandType = CommandType.StoredProcedure;

		//    cmd.Parameters.AddWithValue("@Action", "DELETE");
		//    cmd.Parameters.AddWithValue("@UserId", id);

		//    con.Open();
		//    cmd.ExecuteNonQuery();

		//    Audit(GetUserId(), "DELETE_USER", null, $"UserId={id}");
		//    return Ok("Deleted");
		//}

		// =========================
		// JWT + HELPERS
		// =========================
		private string GenerateJwt(int UserID, string role)
		{
			var jwt = _config.GetSection("Jwt");

			string normalizedRole = role;
			if (role.Equals("Bank Officer", StringComparison.OrdinalIgnoreCase))
			{
				normalizedRole = "Officer";
			}

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, UserID.ToString()),
				new Claim(ClaimTypes.Role, normalizedRole)
			};

			var key = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(jwt["Key"])
			);

			var creds = new SigningCredentials(
				key,
				SecurityAlgorithms.HmacSha256
			);

			var token = new JwtSecurityToken(
				issuer: jwt["Issuer"],
				audience: jwt["Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(
					Convert.ToDouble(jwt["ExpiresMinutes"])
				),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}


		private int GetUserID()
		{
			return int.Parse(
				User.FindFirstValue(ClaimTypes.NameIdentifier));
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
