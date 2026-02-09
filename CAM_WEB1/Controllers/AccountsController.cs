using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using CAM_WEB1.Models;

namespace CAM_WEB1.Controllers
{
	[ApiController]
	[Route("api/v1/[controller]")]
	public class AccountsController : ControllerBase
	{
		private readonly string _conn;

		public AccountsController(IConfiguration configuration)
		{
			_conn = configuration.GetConnectionString("DefaultConnection") ?? "";
		}

		// ==========================================
		// 1. CREATE ACCOUNT 
		// ==========================================
		[HttpPost("create")]
		[Authorize(Roles = "Officer")]
		public IActionResult CreateAccount(Account account)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_Account", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "Create");
			cmd.Parameters.AddWithValue("@Branch", account.Branch);
			cmd.Parameters.AddWithValue("@CustomerName", account.CustomerName);
			cmd.Parameters.AddWithValue("@CustomerID", account.CustomerID);
			cmd.Parameters.AddWithValue("@AccountType", account.AccountType);
			cmd.Parameters.AddWithValue("@Balance", account.Balance);
			cmd.Parameters.AddWithValue("@Status", "Active");
			cmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

			con.Open();
			cmd.ExecuteNonQuery();

			Audit(GetUserID(), "CREATE_ACCOUNT", null, $"CustID: {account.CustomerID}");

			return Ok(new { message = "Account successfully created by Officer" });
		}

		// ==========================================
		// 2. UPDATE ACCOUNT 
		// ==========================================
		[HttpPut("update/{id}")]
		[Authorize(Roles = "Officer")]
		public IActionResult UpdateAccount(int id, Account account)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_Account", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "Update");
			cmd.Parameters.AddWithValue("@AccountID", id);
			cmd.Parameters.AddWithValue("@Branch", account.Branch);
			cmd.Parameters.AddWithValue("@CustomerName", account.CustomerName);
			cmd.Parameters.AddWithValue("@AccountType", account.AccountType);
			cmd.Parameters.AddWithValue("@Status", account.Status);

			con.Open();
			int rows = cmd.ExecuteNonQuery();

			if (rows == 0) return NotFound("Account not found");

			Audit(GetUserID(), "UPDATE_ACCOUNT", $"AccID: {id}", $"NewStatus: {account.Status}");

			return Ok(new { message = "Account details updated successfully" });
		}

		// ==========================================
		// 5. CLOSE ACCOUNT
		// ==========================================
		[HttpPut("close/{id}")]
		[Authorize(Roles = "Officer")]
		public IActionResult CloseAccount(int id)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_Account", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "Close");
			cmd.Parameters.AddWithValue("@AccountID", id);

			con.Open();
			cmd.ExecuteNonQuery();

			Audit(GetUserID(), "CLOSE_ACCOUNT", "Active", $"AccID: {id} Closed");

			return Ok(new { message = "Account status updated to Closed" });
		}

		// ==========================================
		// 3. GET ACCOUNT DETAIL (Existing)
		// ==========================================
		[HttpGet("details/{id}")]
		[Authorize(Roles = "Officer,Manager,Admin")]
		public IActionResult GetAccountDetail(int id)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_Account", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "GetById");
			cmd.Parameters.AddWithValue("@AccountID", id);

			con.Open();
			DataTable dt = new();
			dt.Load(cmd.ExecuteReader());

			if (dt.Rows.Count == 0) return NotFound();
			return Ok(ToList(dt).FirstOrDefault());
		}

		// ==========================================
		// 4. LIST ALL ACCOUNTS (Existing)
		// ==========================================
		[HttpGet("all")]
		[Authorize(Roles = "Officer,Manager,Admin")]
		public IActionResult ListAllAccounts()
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_Account", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@Action", "GetAll");

			con.Open();
			DataTable dt = new();
			dt.Load(cmd.ExecuteReader());

			return Ok(ToList(dt));
		}

		// ==========================================
		// HELPERS (ToList, GetUserID, Audit)
		// ==========================================

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

		private int GetUserID()
		{
			var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
			return claim != null ? int.Parse(claim) : 0;
		}

		private void Audit(int UserID, string action, string oldVal, string newVal)
		{
			using var con = new SqlConnection(_conn);
			using var cmd = new SqlCommand("usp_user_audit", con);
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("@UserId", UserID);
			cmd.Parameters.AddWithValue("@Action", action);
			cmd.Parameters.AddWithValue("@OldValue", (object?)oldVal ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@NewValue", (object?)newVal ?? DBNull.Value);

			con.Open();
			cmd.ExecuteNonQuery();
		}
	}
}