using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microplex.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SmsCredits",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsCredits",
                table: "Clients");
        }
    }
}
