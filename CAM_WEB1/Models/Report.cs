using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
    [Table("t_Report")]
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReportId { get; set; }

        [Required, StringLength(512)]
        // FIXED: Added = string.Empty; to resolve CS8618 Warning
        public string Scope { get; set; } = string.Empty;

        [Required]
        // FIXED: Added = string.Empty; to resolve CS8618 Warning
        public string Metrics { get; set; } = string.Empty;

        [Required]
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }

    [Table("t_Report_Audit")]
    public class ReportAudit
    {
        [Key]
        public int AuditID { get; set; }
        public long ReportId { get; set; }
        public string Action { get; set; } = "Generated";
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}