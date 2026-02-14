using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
    // Coding Standard: table prefix t_
    [Table("t_Approval")]
    public class Approval
    {
        [Key]
        public int ApprovalID { get; set; }             // PK

        [Required]
        public int TransactionID { get; set; }          // FK -> Transaction.TransactionID
        [Required]
        public int ReviewerID { get; set; }             // FK -> User.UserID

        // Allowed: Pending | Approve | Reject
        [Required, StringLength(10)]
        public string Decision { get; set; } = "Pending";

        [StringLength(1024)]
        public string? Comments { get; set; }

        // Store in UTC
        public DateTime ApprovalDate { get; set; } = DateTime.UtcNow;
    }

   
}
