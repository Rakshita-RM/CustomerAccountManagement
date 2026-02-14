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

        public int ReportID { get; set; }

        [Required, StringLength(100)]

        // --- CHANGE HERE: Renamed from Scope to Branch ---

        public string Branch { get; set; } = "Global";

        [Required]

        public int TotalTransactions { get; set; }

        [Required]

        public int HighValueCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]

        public decimal AccountGrowthRate { get; set; }

        [Required]

        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

    }

    [Table("t_Report_Audit")]

    public class ReportAudit

    {

        [Key]

        public int AuditID { get; set; }

        [Required]

        public int ReportID { get; set; }

        [Required, StringLength(50)]

        public string Action { get; set; } = "GENERATE";

        [Required]

        public int UserID { get; set; }

        [Required]

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    }

}
