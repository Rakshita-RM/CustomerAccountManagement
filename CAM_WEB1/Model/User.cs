using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
    // Coding Standard: t_ prefix for tables
    [Table("t_User")]
    public class User
    {
        [Key]
        public int UserID { get; set; }              // PK

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Officer | Manager | Admin  (keep as string to match your PDF; you can switch to enum later)
        [Required, StringLength(20)]
        public string Role { get; set; } = "Officer";

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Branch { get; set; }

        // Active | Inactive
        [Required, StringLength(10)]
        public string Status { get; set; } = "Active";
    }
}
