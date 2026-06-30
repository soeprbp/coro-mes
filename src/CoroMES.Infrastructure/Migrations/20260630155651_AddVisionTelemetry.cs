using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoroMES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisionTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisionSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalSystemId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EndpointBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DashboardBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EquipmentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCollectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionSources_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VisionCameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisionSourceId = table.Column<int>(type: "integer", nullable: false),
                    ElementId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SlotId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EquipmentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastMotionLevel = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionCameras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionCameras_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VisionCameras_VisionSources_VisionSourceId",
                        column: x => x.VisionSourceId,
                        principalTable: "VisionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisionZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisionSourceId = table.Column<int>(type: "integer", nullable: false),
                    VisionCameraId = table.Column<int>(type: "integer", nullable: true),
                    ElementId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastMotionPercent = table.Column<double>(type: "double precision", nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionZones_VisionCameras_VisionCameraId",
                        column: x => x.VisionCameraId,
                        principalTable: "VisionCameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VisionZones_VisionSources_VisionSourceId",
                        column: x => x.VisionSourceId,
                        principalTable: "VisionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisionEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisionSourceId = table.Column<int>(type: "integer", nullable: false),
                    VisionCameraId = table.Column<int>(type: "integer", nullable: true),
                    VisionZoneId = table.Column<int>(type: "integer", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MotionPercent = table.Column<double>(type: "double precision", nullable: true),
                    Region = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RawJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionEvents_VisionCameras_VisionCameraId",
                        column: x => x.VisionCameraId,
                        principalTable: "VisionCameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VisionEvents_VisionSources_VisionSourceId",
                        column: x => x.VisionSourceId,
                        principalTable: "VisionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisionEvents_VisionZones_VisionZoneId",
                        column: x => x.VisionZoneId,
                        principalTable: "VisionZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VisionReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisionSourceId = table.Column<int>(type: "integer", nullable: false),
                    VisionCameraId = table.Column<int>(type: "integer", nullable: true),
                    VisionZoneId = table.Column<int>(type: "integer", nullable: true),
                    ElementId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metric = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NumericValue = table.Column<double>(type: "double precision", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true),
                    Quality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RawJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisionReadings_VisionCameras_VisionCameraId",
                        column: x => x.VisionCameraId,
                        principalTable: "VisionCameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VisionReadings_VisionSources_VisionSourceId",
                        column: x => x.VisionSourceId,
                        principalTable: "VisionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisionReadings_VisionZones_VisionZoneId",
                        column: x => x.VisionZoneId,
                        principalTable: "VisionZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisionCameras_EquipmentId",
                table: "VisionCameras",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionCameras_VisionSourceId_ElementId",
                table: "VisionCameras",
                columns: new[] { "VisionSourceId", "ElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisionEvents_OccurredAtUtc",
                table: "VisionEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VisionEvents_VisionCameraId",
                table: "VisionEvents",
                column: "VisionCameraId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionEvents_VisionSourceId_OccurredAtUtc_Status",
                table: "VisionEvents",
                columns: new[] { "VisionSourceId", "OccurredAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VisionEvents_VisionZoneId",
                table: "VisionEvents",
                column: "VisionZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionReadings_OccurredAtUtc",
                table: "VisionReadings",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VisionReadings_VisionCameraId",
                table: "VisionReadings",
                column: "VisionCameraId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionReadings_VisionSourceId_ElementId_Metric_OccurredAtUtc",
                table: "VisionReadings",
                columns: new[] { "VisionSourceId", "ElementId", "Metric", "OccurredAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisionReadings_VisionZoneId",
                table: "VisionReadings",
                column: "VisionZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionSources_EquipmentId",
                table: "VisionSources",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionSources_ExternalSystemId",
                table: "VisionSources",
                column: "ExternalSystemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisionSources_IsActive",
                table: "VisionSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VisionZones_VisionCameraId",
                table: "VisionZones",
                column: "VisionCameraId");

            migrationBuilder.CreateIndex(
                name: "IX_VisionZones_VisionSourceId_ElementId",
                table: "VisionZones",
                columns: new[] { "VisionSourceId", "ElementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisionEvents");

            migrationBuilder.DropTable(
                name: "VisionReadings");

            migrationBuilder.DropTable(
                name: "VisionZones");

            migrationBuilder.DropTable(
                name: "VisionCameras");

            migrationBuilder.DropTable(
                name: "VisionSources");
        }
    }
}
