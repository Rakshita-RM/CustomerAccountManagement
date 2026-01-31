using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CAM_WEB1.Migrations
{
    /// <inheritdoc />
    public partial class InitialReportingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_Account",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Account", x => x.AccountID);
                });

            migrationBuilder.CreateTable(
                name: "t_Approval",
                columns: table => new
                {
                    ApprovalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionID = table.Column<int>(type: "int", nullable: false),
                    ReviewerID = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Approval", x => x.ApprovalID);
                });

            migrationBuilder.CreateTable(
                name: "t_Report",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Report", x => x.ReportId);
                });

            migrationBuilder.CreateTable(
                name: "t_Report_Audit",
                columns: table => new
                {
                    AuditID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Report_Audit", x => x.AuditID);
                });

            migrationBuilder.CreateTable(
                name: "t_Transaction",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Transaction", x => x.TransactionID);
                });

            migrationBuilder.CreateTable(
                name: "t_User",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_User", x => x.UserID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_Account_Status_CreatedDate",
                table: "t_Account",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_t_Transaction_AccountID",
                table: "t_Transaction",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_t_Transaction_Date_Status",
                table: "t_Transaction",
                columns: new[] { "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_t_User_Email",
                table: "t_User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_User_Role",
                table: "t_User",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_t_User_Status",
                table: "t_User",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_Account");

            migrationBuilder.DropTable(
                name: "t_Approval");

            migrationBuilder.DropTable(
                name: "t_Report");

            migrationBuilder.DropTable(
                name: "t_Report_Audit");

            migrationBuilder.DropTable(
                name: "t_Transaction");

            migrationBuilder.DropTable(
                name: "t_User");
        }
    }
}
