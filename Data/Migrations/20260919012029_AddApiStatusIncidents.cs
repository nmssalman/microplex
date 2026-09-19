using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microplex.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiStatusIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiStatusIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiStatusIncidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiStatusIncidents_OccurredAtUtc",
                table: "ApiStatusIncidents",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiStatusIncidents");
        }
    }
}
