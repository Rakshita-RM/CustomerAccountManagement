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

    // Audit entity following your convention t_{Entity}_Audit
    [Table("t_Approval_Audit")]
    public class ApprovalAudit
    {
        [Key]
        public int AuditID { get; set; }                // PK of audit row

        public int ApprovalID { get; set; }
        public int TransactionID { get; set; }
        public int ReviewerID { get; set; }
        public string? Decision { get; set; }
        public string? Comments { get; set; }
        public DateTime ApprovalDate { get; set; }

        // Added | Modified | Deleted
        [Required, StringLength(16)]
        public string Action { get; set; } = "Added";

        // UTC timestamp of the change
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}
