using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient; 
using CAM_WEB1.Data;
using CAM_WEB1.Models;

namespace CAM_WEB1.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public AccountsController(ApplicationDbContext context)
		{
			_context = context;
		}

		// CREATE: api/Accounts
		[HttpPost]
		public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
		{
			var parameters = new[] {
				new SqlParameter("@Action", "Create"),
				new SqlParameter("@Branch", account.Branch ?? (object)DBNull.Value),
				new SqlParameter("@CustomerName", account.CustomerName),
				new SqlParameter("@CustomerID", account.CustomerID),
				new SqlParameter("@AccountType", account.AccountType),
				new SqlParameter("@Balance", account.Balance),
				new SqlParameter("@Status", account.Status),
				new SqlParameter("@CreatedDate", account.CreatedDate == default ? DateTime.UtcNow : account.CreatedDate)
			};

			// Executes sp_Account and returns the newly created record
			var result = await _context.Accounts
				.FromSqlRaw("EXEC dbo.sp_Account @Action, NULL, @Branch, @CustomerName, @CustomerID, @AccountType, @Balance, @Status, @CreatedDate", parameters)
				.ToListAsync();

			var newAccount = result.FirstOrDefault();
			return CreatedAtAction(nameof(GetAccountById), new { id = newAccount?.AccountID }, newAccount);
		}

		// GET ALL: api/Accounts
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Account>>> GetAllAccounts()
		{
			return await _context.Accounts
				.FromSqlRaw("EXEC dbo.sp_Account @Action = 'GetAll'")
				.ToListAsync();
		}

		// GET BY ID: api/Accounts/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Account>> GetAccountById(int id)
		{
			// Ensure you are calling the 'GetById' action defined in your SP
			var result = await _context.Accounts
				.FromSqlRaw("EXEC dbo.sp_Account @Action = 'GetById', @AccountID = {0}", id)
				.ToListAsync();

			var account = result.FirstOrDefault();
			if (account == null) return NotFound();
			return account;
		}

		// UPDATE: api/Accounts/5
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateAccount(int id, [FromBody] Account account)
		{
			if (id != account.AccountID) return BadRequest("ID Mismatch");

			var parameters = new[] {
				new SqlParameter("@Action", "Update"),
				new SqlParameter("@AccountID", id),
				new SqlParameter("@Branch", account.Branch),
				new SqlParameter("@CustomerName", account.CustomerName),
				new SqlParameter("@CustomerID", account.CustomerID),
				new SqlParameter("@AccountType", account.AccountType),
				new SqlParameter("@Balance", account.Balance),
				new SqlParameter("@Status", account.Status)
			};

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC dbo.sp_Account @Action, @AccountID, @Branch, @CustomerName, @CustomerID, @AccountType, @Balance, @Status",
				parameters);

			return NoContent();
		}

		// DELETE: api/Accounts/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAccount(int id)
		{
			await _context.Database.ExecuteSqlRawAsync("EXEC dbo.sp_Account @Action = 'Delete', @AccountID = {0}", id);
			return NoContent();
		}
	}
}