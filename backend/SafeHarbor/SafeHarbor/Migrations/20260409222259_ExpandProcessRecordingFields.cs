using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProcessRecordingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConcernsFlagged",
                table: "ProcessRecordings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmotionalStateEnd",
                table: "ProcessRecordings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmotionalStateObserved",
                table: "ProcessRecordings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpActions",
                table: "ProcessRecordings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterventionsApplied",
                table: "ProcessRecordings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotesRestricted",
                table: "ProcessRecordings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProgressNoted",
                table: "ProcessRecordings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReferralMade",
                table: "ProcessRecordings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SessionDurationMinutes",
                table: "ProcessRecordings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                table: "ProcessRecordings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SocialWorker",
                table: "ProcessRecordings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcernsFlagged",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "EmotionalStateEnd",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "EmotionalStateObserved",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "FollowUpActions",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "InterventionsApplied",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "NotesRestricted",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "ProgressNoted",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "ReferralMade",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "SessionDurationMinutes",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "ProcessRecordings");

            migrationBuilder.DropColumn(
                name: "SocialWorker",
                table: "ProcessRecordings");
        }
    }
}
