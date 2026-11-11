using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupSwitch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "ScheduledSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("UPDATE ScheduledSessions SET AcademicYear = '2026-2027' WHERE AcademicYear IS NULL");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "ImportRuns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CancellationRequested",
                table: "ImportRuns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FirstWeek",
                table: "ImportRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastWeek",
                table: "ImportRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedWeeks",
                table: "ImportRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestedGroupName",
                table: "ImportRuns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulWeeks",
                table: "ImportRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ScheduleCoverages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentGroupId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionCount = table.Column<int>(type: "int", nullable: false),
                    LastImportRunId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleCoverages_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleCoverages_StudentGroupId_AcademicYear_WeekNumber",
                table: "ScheduleCoverages",
                columns: new[] { "StudentGroupId", "AcademicYear", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleCoverages");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "ScheduledSessions");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "CancellationRequested",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "FirstWeek",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "LastWeek",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "RejectedWeeks",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "RequestedGroupName",
                table: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "SuccessfulWeeks",
                table: "ImportRuns");
        }
    }
}
