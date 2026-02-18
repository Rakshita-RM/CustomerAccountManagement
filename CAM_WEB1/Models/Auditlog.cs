using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
    [Table("t_user_audit")]
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }

        public int? UserId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? Action { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [Required]
        public DateTime PerformedDate { get; set; }
    }
}
