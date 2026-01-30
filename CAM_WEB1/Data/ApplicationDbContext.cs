using CAM_WEB1.Models;
using Microsoft.EntityFrameworkCore;

namespace CAM_WEB1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; } // Add this line
        public DbSet<Approval> Approvals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("t_User"); // t_ prefix
                entity.HasKey(u => u.UserID);

                entity.Property(u => u.Name).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Role).HasMaxLength(20).IsRequired();

                entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
                entity.Property(u => u.Branch).HasMaxLength(100);
                entity.Property(u => u.Status).HasMaxLength(10).IsRequired();

                entity.HasIndex(u => u.Email).IsUnique(); // optional but common
                entity.HasIndex(u => u.Role);
                entity.HasIndex(u => u.Status);
            });


            // --- Account mapping (adjust/remove ToTable if already mapped via [Table]) ---
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("t_Account");     // keep your t_ naming standard
                entity.HasKey(a => a.AccountID);
            });

            // --- Transaction mapping ---
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("t_Transaction"); // keep your t_ naming standard
                entity.HasKey(t => t.TransactionID);
            });

            // --- Approval mapping (matches PDF fields) ---
            modelBuilder.Entity<Approval>(entity =>
            {
                entity.ToTable("t_Approval");    // Coding Standard: t_ prefix
                entity.HasKey(a => a.ApprovalID);

                entity.Property(a => a.Decision)
                      .HasMaxLength(10)
                      .IsRequired();

                entity.Property(a => a.Comments)
                      .HasMaxLength(1024);


            });
        }
    }
}