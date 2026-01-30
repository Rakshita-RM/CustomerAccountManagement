using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CAM_WEB1.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_t_Transactions",
                table: "t_Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_Accounts",
                table: "t_Accounts");

            migrationBuilder.RenameTable(
                name: "t_Transactions",
                newName: "t_Transaction");

            migrationBuilder.RenameTable(
                name: "t_Accounts",
                newName: "t_Account");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_Transaction",
                table: "t_Transaction",
                column: "TransactionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_Account",
                table: "t_Account",
                column: "AccountID");

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
                name: "t_Approval");

            migrationBuilder.DropTable(
                name: "t_User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_Transaction",
                table: "t_Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_Account",
                table: "t_Account");

            migrationBuilder.RenameTable(
                name: "t_Transaction",
                newName: "t_Transactions");

            migrationBuilder.RenameTable(
                name: "t_Account",
                newName: "t_Accounts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_Transactions",
                table: "t_Transactions",
                column: "TransactionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_Accounts",
                table: "t_Accounts",
                column: "AccountID");
        }
    }
}
