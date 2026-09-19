using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microplex.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    LastCheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiStatuses_ApiName_MethodName",
                table: "ApiStatuses",
                columns: new[] { "ApiName", "MethodName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiStatuses");
        }
    }
}
