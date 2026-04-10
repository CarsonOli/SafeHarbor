using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeVisitDetailsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "family_cooperation_level",
                schema: "lighthouse",
                table: "home_visits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "follow_up_actions",
                schema: "lighthouse",
                table: "home_visits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "home_environment_observations",
                schema: "lighthouse",
                table: "home_visits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "safety_concerns_identified",
                schema: "lighthouse",
                table: "home_visits",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "family_cooperation_level",
                schema: "lighthouse",
                table: "home_visits");

            migrationBuilder.DropColumn(
                name: "follow_up_actions",
                schema: "lighthouse",
                table: "home_visits");

            migrationBuilder.DropColumn(
                name: "home_environment_observations",
                schema: "lighthouse",
                table: "home_visits");

            migrationBuilder.DropColumn(
                name: "safety_concerns_identified",
                schema: "lighthouse",
                table: "home_visits");
        }
    }
}
