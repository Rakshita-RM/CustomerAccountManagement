using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
	//[Table("t_Accounts")] // Coding Standard: t_ prefix for tables
    [Table("t_Account")] // Coding Standard: t_ prefix for tables
    public class Account
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int AccountID { get; set; } // [cite: 46]

        public string Branch { get; set; } = "Main Branch";

        [Required]
		[StringLength(100)]
		public string CustomerName { get; set; } = string.Empty; // [cite: 48]

		[Required]
		[StringLength(50)]
		public string CustomerID { get; set; } = string.Empty; // [cite: 50]

		[Required]
		public string AccountType { get; set; } = "Savings"; // Options: Savings/Current [cite: 51]

		[Column(TypeName = "decimal(18,2)")]
		public decimal Balance { get; set; } = 0.00m; // [cite: 52]

		[Required]
		public string Status { get; set; } = "Active"; // Options: Active/Closed [cite: 53]

		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		//[Required]
		//public string Role { get; set; } = "BankOfficer";
	}
}