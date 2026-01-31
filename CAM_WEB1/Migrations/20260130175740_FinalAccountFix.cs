using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CAM_WEB1.Migrations
{
    /// <inheritdoc />
    public partial class FinalAccountFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "t_Account",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branch",
                table: "t_Account");
        }
    }
}
