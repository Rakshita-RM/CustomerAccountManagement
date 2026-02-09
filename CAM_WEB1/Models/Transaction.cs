using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
	[Table("t_Transactions")] // Following your 't_' coding standard
	public class Transaction
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int TransactionID { get; set; }

		[Required]
		public int AccountID { get; set; }

		[Required]
		public string Type { get; set; } = string.Empty; // Deposit, Withdrawal, or Transfer [cite: 63]

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		public DateTime Date { get; set; } = DateTime.UtcNow; 

		public string Status { get; set; } = "Completed"; // Completed or Pending [cite: 67]
	}
}