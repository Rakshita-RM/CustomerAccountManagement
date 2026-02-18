using CAM_WEB1.Models;

using Microsoft.EntityFrameworkCore;

using CAM_WEB1.DTO;

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

        //public DbSet<RefreshTokenRequest> Refresh  { get; set; }

        public DbSet<AuditLog> AuditLog { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportAudit> ReportAudits { get; set; }

        



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


                entity.Property(a => a.Branch).HasMaxLength(100);

                // Helps AccountGrowthRate and branch/type filters
                entity.HasIndex(a => new
                {
                    a.Status,
                    a.CreatedDate
                }); 
            });


            // --- Transaction mapping ---
            modelBuilder.Entity<Transaction>(entity =>
                {
                    entity.ToTable("t_Transaction"); // keep your t_ naming standard
                    entity.HasKey(t => t.TransactionID);

                    // Helps date-range filtering and join to Account
                    entity.HasIndex(t => new { t.Date, t.Status });
                    entity.HasIndex(t => t.AccountID);
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

            // 1. Report Mapping
            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("t_Report");

                entity.HasKey(r => r.ReportID);

                entity.Property(r => r.Branch)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(r => r.TotalTransactions)
                      .IsRequired();

                entity.Property(r => r.HighValueCount)
                      .IsRequired();

                entity.Property(r => r.AccountGrowthRate)
                      .HasColumnType("decimal(18,2)");

                entity.Property(r => r.GeneratedDate)
                      .HasDefaultValueSql("GETUTCDATE()");
            });


            // --- Report mapping (correct as you have it) ---
            // 2. Report Audit Mapping
            modelBuilder.Entity<ReportAudit>(entity =>
            {
                entity.ToTable("t_Report_Audit"); // or t_Report_Audit

                entity.HasKey(ra => ra.AuditID);

                entity.Property(ra => ra.ReportID)
                      .IsRequired();

                entity.Property(ra => ra.Action)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(ra => ra.UserID)
                      .IsRequired();

                entity.Property(ra => ra.ActionDate)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Optional FK relationship
                entity.HasOne<Report>()
                      .WithMany()
                      .HasForeignKey(ra => ra.ReportID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("t_user_audit");

                entity.HasKey(e => e.AuditId);

                entity.Property(e => e.AuditId)
                      .HasColumnName("AuditId");

                entity.Property(e => e.UserId)
                      .HasColumnName("UserId");

                entity.Property(e => e.Action)
                      .HasMaxLength(50)
                      .HasColumnType("nvarchar(50)");

                entity.Property(e => e.OldValue)
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.NewValue)
                      .HasColumnType("nvarchar(max)");

                entity.Property(e => e.PerformedDate)
                      .HasColumnType("datetime")
                      .IsRequired();
            });
        }
    }
}
            


            