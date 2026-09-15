using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microplex.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmsApiKey",
                table: "Clients",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_SmsApiKey",
                table: "Clients",
                column: "SmsApiKey",
                unique: true,
                filter: "[SmsApiKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_SmsApiKey",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SmsApiKey",
                table: "Clients");
        }
    }
}
