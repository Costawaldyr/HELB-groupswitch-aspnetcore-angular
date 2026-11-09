using GroupSwitch.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupSwitch.Data.Migrations
{
    /// <inheritdoc />
    public partial class WidenRoomAndTeacher : Migration
    {
        private const int PreviousRoomMaxLength = 100;
        private const int PreviousTeacherMaxLength = 200;
        private const string ScheduledSessionsTable = "ScheduledSessions";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: nameof(ScheduledSession.Teacher),
                table: ScheduledSessionsTable,
                type: $"nvarchar({ScheduledSession.TeacherMaxLength})",
                maxLength: ScheduledSession.TeacherMaxLength,
                nullable: false,
                oldClrType: typeof(string),
                oldType: $"nvarchar({PreviousTeacherMaxLength})",
                oldMaxLength: PreviousTeacherMaxLength);

            migrationBuilder.AlterColumn<string>(
                name: nameof(ScheduledSession.Room),
                table: ScheduledSessionsTable,
                type: $"nvarchar({ScheduledSession.RoomMaxLength})",
                maxLength: ScheduledSession.RoomMaxLength,
                nullable: false,
                oldClrType: typeof(string),
                oldType: $"nvarchar({PreviousRoomMaxLength})",
                oldMaxLength: PreviousRoomMaxLength);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: nameof(ScheduledSession.Teacher),
                table: ScheduledSessionsTable,
                type: $"nvarchar({PreviousTeacherMaxLength})",
                maxLength: PreviousTeacherMaxLength,
                nullable: false,
                oldClrType: typeof(string),
                oldType: $"nvarchar({ScheduledSession.TeacherMaxLength})",
                oldMaxLength: ScheduledSession.TeacherMaxLength);

            migrationBuilder.AlterColumn<string>(
                name: nameof(ScheduledSession.Room),
                table: ScheduledSessionsTable,
                type: $"nvarchar({PreviousRoomMaxLength})",
                maxLength: PreviousRoomMaxLength,
                nullable: false,
                oldClrType: typeof(string),
                oldType: $"nvarchar({ScheduledSession.RoomMaxLength})",
                oldMaxLength: ScheduledSession.RoomMaxLength);
        }
    }
}
