using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAM_WEB1.Models
{
    [Table("t_user")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Role { get; set; } = "Officer";

        [Required, StringLength(50)]
        public string Branch { get; set; } ="Chennai";

        [Required, StringLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
