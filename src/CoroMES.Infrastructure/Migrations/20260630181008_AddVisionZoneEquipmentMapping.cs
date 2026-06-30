using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoroMES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisionZoneEquipmentMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "VisionZones",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisionZones_EquipmentId",
                table: "VisionZones",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_VisionZones_Equipment_EquipmentId",
                table: "VisionZones",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VisionZones_Equipment_EquipmentId",
                table: "VisionZones");

            migrationBuilder.DropIndex(
                name: "IX_VisionZones_EquipmentId",
                table: "VisionZones");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "VisionZones");
        }
    }
}
