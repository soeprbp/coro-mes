using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoroMES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserScopedSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SystemSettings",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UserId_Key",
                table: "SystemSettings",
                columns: new[] { "UserId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_UserId_Key",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SystemSettings");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }
    }
}
