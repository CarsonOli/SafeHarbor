using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionAllocation_Contributions_ContributionId",
                table: "ContributionAllocation");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributionAllocation_Safehouses_SafehouseId",
                table: "ContributionAllocation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributionAllocation",
                table: "ContributionAllocation");

            migrationBuilder.RenameTable(
                name: "ContributionAllocation",
                newName: "ContributionAllocations");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionAllocation_SafehouseId",
                table: "ContributionAllocations",
                newName: "IX_ContributionAllocations_SafehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionAllocation_ContributionId",
                table: "ContributionAllocations",
                newName: "IX_ContributionAllocations_ContributionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributionAllocations",
                table: "ContributionAllocations",
                column: "Id");
            // NOTE: This migration intentionally keeps auth persistence on lighthouse.users only;
            // no ASP.NET Identity tables are created here.

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionAllocations_Contributions_ContributionId",
                table: "ContributionAllocations",
                column: "ContributionId",
                principalTable: "Contributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionAllocations_Safehouses_SafehouseId",
                table: "ContributionAllocations",
                column: "SafehouseId",
                principalTable: "Safehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionAllocations_Contributions_ContributionId",
                table: "ContributionAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributionAllocations_Safehouses_SafehouseId",
                table: "ContributionAllocations");
            // NOTE: Down only reverts the table rename/foreign-key updates from this migration.
            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributionAllocations",
                table: "ContributionAllocations");

            migrationBuilder.RenameTable(
                name: "ContributionAllocations",
                newName: "ContributionAllocation");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionAllocations_SafehouseId",
                table: "ContributionAllocation",
                newName: "IX_ContributionAllocation_SafehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionAllocations_ContributionId",
                table: "ContributionAllocation",
                newName: "IX_ContributionAllocation_ContributionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributionAllocation",
                table: "ContributionAllocation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionAllocation_Contributions_ContributionId",
                table: "ContributionAllocation",
                column: "ContributionId",
                principalTable: "Contributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionAllocation_Safehouses_SafehouseId",
                table: "ContributionAllocation",
                column: "SafehouseId",
                principalTable: "Safehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
