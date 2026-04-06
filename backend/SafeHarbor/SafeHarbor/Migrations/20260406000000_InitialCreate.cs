using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SafeHarbor.Models.Enums;

#nullable disable

namespace SafeHarbor.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_CaseCategories", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "ContributionTypes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_ContributionTypes", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Donors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                LastActivityAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Donors", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "OutcomeSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SnapshotDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                TotalResidentsServed = table.Column<int>(type: "INTEGER", nullable: false),
                TotalHomeVisits = table.Column<int>(type: "INTEGER", nullable: false),
                TotalContributions = table.Column<decimal>(type: "TEXT", nullable: false),
                CampaignEngagementRate = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_OutcomeSnapshots", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Roles", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Safehouses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Region = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Safehouses", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "StatusStates",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Domain = table.Column<int>(type: "INTEGER", nullable: false),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_StatusStates", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "VisitTypes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_VisitTypes", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Campaigns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Campaigns", x => x.Id);
                table.ForeignKey("FK_Campaigns_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CaseSubcategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CaseCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseSubcategories", x => x.Id);
                table.ForeignKey("FK_CaseSubcategories_CaseCategories_CaseCategoryId", x => x.CaseCategoryId, "CaseCategories", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Contributions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                DonorId = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: true),
                ContributionTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                ContributionDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contributions", x => x.Id);
                table.ForeignKey("FK_Contributions_Campaigns_CampaignId", x => x.CampaignId, "Campaigns", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Contributions_ContributionTypes_ContributionTypeId", x => x.ContributionTypeId, "ContributionTypes", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Contributions_Donors_DonorId", x => x.DonorId, "Donors", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Contributions_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ResidentCases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SafehouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                CaseCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                CaseSubcategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResidentCases", x => x.Id);
                table.ForeignKey("FK_ResidentCases_CaseCategories_CaseCategoryId", x => x.CaseCategoryId, "CaseCategories", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ResidentCases_CaseSubcategories_CaseSubcategoryId", x => x.CaseSubcategoryId, "CaseSubcategories", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ResidentCases_Safehouses_SafehouseId", x => x.SafehouseId, "Safehouses", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ResidentCases_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ResidentCases_Users_ResidentUserId", x => x.ResidentUserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "SocialPostMetrics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                MetricDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Reach = table.Column<int>(type: "INTEGER", nullable: false),
                Engagements = table.Column<int>(type: "INTEGER", nullable: false),
                Clicks = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SocialPostMetrics", x => x.Id);
                table.ForeignKey("FK_SocialPostMetrics_Campaigns_CampaignId", x => x.CampaignId, "Campaigns", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                RoleId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey("FK_UserRoles_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_UserRoles_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ContributionAllocations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ContributionId = table.Column<Guid>(type: "TEXT", nullable: false),
                SafehouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                AmountAllocated = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContributionAllocations", x => x.Id);
                table.ForeignKey("FK_ContributionAllocations_Contributions_ContributionId", x => x.ContributionId, "Contributions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ContributionAllocations_Safehouses_SafehouseId", x => x.SafehouseId, "Safehouses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CaseConferences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                ConferenceDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                OutcomeSummary = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseConferences", x => x.Id);
                table.ForeignKey("FK_CaseConferences_ResidentCases_ResidentCaseId", x => x.ResidentCaseId, "ResidentCases", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_CaseConferences_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "HomeVisits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                VisitTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                VisitDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HomeVisits", x => x.Id);
                table.ForeignKey("FK_HomeVisits_ResidentCases_ResidentCaseId", x => x.ResidentCaseId, "ResidentCases", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_HomeVisits_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_HomeVisits_VisitTypes_VisitTypeId", x => x.VisitTypeId, "VisitTypes", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InterventionPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                EffectiveFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                EffectiveTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                PlanDetails = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterventionPlans", x => x.Id);
                table.ForeignKey("FK_InterventionPlans_ResidentCases_ResidentCaseId", x => x.ResidentCaseId, "ResidentCases", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_InterventionPlans_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProcessRecordings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Summary = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcessRecordings", x => x.Id);
                table.ForeignKey("FK_ProcessRecordings_ResidentCases_ResidentCaseId", x => x.ResidentCaseId, "ResidentCases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResidentAssessments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ResidentCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                AssessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                StatusStateId = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResidentAssessments", x => x.Id);
                table.ForeignKey("FK_ResidentAssessments_ResidentCases_ResidentCaseId", x => x.ResidentCaseId, "ResidentCases", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ResidentAssessments_StatusStates_StatusStateId", x => x.StatusStateId, "StatusStates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "CaseCategories",
            columns: ["Id", "Code", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy"],
            values: new object[,]
            {
                { 1, "SAFETY", "Safety and Protection", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 2, "HEALTH", "Health and Wellness", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 3, "LEGAL", "Legal and Documentation", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" }
            });

        migrationBuilder.InsertData(
            table: "ContributionTypes",
            columns: ["Id", "Code", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy"],
            values: new object[,]
            {
                { 1, "ONE_TIME", "One-time Gift", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 2, "RECURRING", "Recurring Gift", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 3, "IN_KIND", "In-kind Support", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" }
            });

        migrationBuilder.InsertData(
            table: "VisitTypes",
            columns: ["Id", "Code", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy"],
            values: new object[,]
            {
                { 1, "INTAKE", "Intake Visit", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 2, "FOLLOWUP", "Follow-up Visit", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 3, "EXIT", "Exit Visit", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" }
            });

        migrationBuilder.InsertData(
            table: "StatusStates",
            columns: ["Id", "Domain", "Code", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy"],
            values: new object[,]
            {
                { 1, (int)StatusDomain.ResidentCase, "OPEN", "Open", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 2, (int)StatusDomain.ResidentCase, "ACTIVE", "Active", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 3, (int)StatusDomain.ResidentCase, "CLOSED", "Closed", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 4, (int)StatusDomain.ResidentAssessment, "DRAFT", "Draft", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 5, (int)StatusDomain.ResidentAssessment, "FINAL", "Finalized", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 6, (int)StatusDomain.HomeVisit, "SCHEDULED", "Scheduled", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 7, (int)StatusDomain.HomeVisit, "COMPLETED", "Completed", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 8, (int)StatusDomain.CaseConference, "PLANNED", "Planned", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 9, (int)StatusDomain.CaseConference, "HELD", "Held", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 10, (int)StatusDomain.InterventionPlan, "PROPOSED", "Proposed", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 11, (int)StatusDomain.InterventionPlan, "APPROVED", "Approved", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 12, (int)StatusDomain.Campaign, "DRAFT", "Draft", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 13, (int)StatusDomain.Campaign, "LIVE", "Live", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 14, (int)StatusDomain.Campaign, "ENDED", "Ended", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 15, (int)StatusDomain.Contribution, "PLEDGED", "Pledged", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 16, (int)StatusDomain.Contribution, "RECEIVED", "Received", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" }
            });

        migrationBuilder.InsertData(
            table: "CaseSubcategories",
            columns: ["Id", "CaseCategoryId", "Code", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy"],
            values: new object[,]
            {
                { 1, 1, "DV", "Domestic Violence", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 2, 1, "TRAFF", "Human Trafficking", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 3, 2, "MH", "Mental Health", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" },
                { 4, 3, "IMM", "Immigration Support", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null, "seed" }
            });

        migrationBuilder.CreateIndex(name: "IX_Campaigns_StatusStateId", table: "Campaigns", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_CaseCategories_Code", table: "CaseCategories", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_CaseConferences_ConferenceDate", table: "CaseConferences", column: "ConferenceDate");
        migrationBuilder.CreateIndex(name: "IX_CaseConferences_ResidentCaseId", table: "CaseConferences", column: "ResidentCaseId");
        migrationBuilder.CreateIndex(name: "IX_CaseConferences_StatusStateId", table: "CaseConferences", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_CaseSubcategories_CaseCategoryId", table: "CaseSubcategories", column: "CaseCategoryId");
        migrationBuilder.CreateIndex(name: "IX_CaseSubcategories_Code", table: "CaseSubcategories", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_ContributionAllocations_ContributionId", table: "ContributionAllocations", column: "ContributionId");
        migrationBuilder.CreateIndex(name: "IX_ContributionAllocations_SafehouseId", table: "ContributionAllocations", column: "SafehouseId");
        migrationBuilder.CreateIndex(name: "IX_Contributions_CampaignId", table: "Contributions", column: "CampaignId");
        migrationBuilder.CreateIndex(name: "IX_Contributions_ContributionDate", table: "Contributions", column: "ContributionDate");
        migrationBuilder.CreateIndex(name: "IX_Contributions_ContributionTypeId", table: "Contributions", column: "ContributionTypeId");
        migrationBuilder.CreateIndex(name: "IX_Contributions_DonorId", table: "Contributions", column: "DonorId");
        migrationBuilder.CreateIndex(name: "IX_Contributions_StatusStateId", table: "Contributions", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_ContributionTypes_Code", table: "ContributionTypes", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Donors_LastActivityAt", table: "Donors", column: "LastActivityAt");
        migrationBuilder.CreateIndex(name: "IX_HomeVisits_ResidentCaseId", table: "HomeVisits", column: "ResidentCaseId");
        migrationBuilder.CreateIndex(name: "IX_HomeVisits_StatusStateId", table: "HomeVisits", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_HomeVisits_VisitTypeId", table: "HomeVisits", column: "VisitTypeId");
        migrationBuilder.CreateIndex(name: "IX_InterventionPlans_ResidentCaseId", table: "InterventionPlans", column: "ResidentCaseId");
        migrationBuilder.CreateIndex(name: "IX_InterventionPlans_StatusStateId", table: "InterventionPlans", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_ProcessRecordings_ResidentCaseId", table: "ProcessRecordings", column: "ResidentCaseId");
        migrationBuilder.CreateIndex(name: "IX_ResidentAssessments_ResidentCaseId", table: "ResidentAssessments", column: "ResidentCaseId");
        migrationBuilder.CreateIndex(name: "IX_ResidentAssessments_StatusStateId", table: "ResidentAssessments", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_ResidentCases_CaseCategoryId", table: "ResidentCases", column: "CaseCategoryId");
        migrationBuilder.CreateIndex(name: "IX_ResidentCases_CaseSubcategoryId", table: "ResidentCases", column: "CaseSubcategoryId");
        migrationBuilder.CreateIndex(name: "IX_ResidentCases_SafehouseId", table: "ResidentCases", column: "SafehouseId");
        migrationBuilder.CreateIndex(name: "IX_ResidentCases_StatusStateId", table: "ResidentCases", column: "StatusStateId");
        migrationBuilder.CreateIndex(name: "IX_ResidentCases_ResidentUserId", table: "ResidentCases", column: "ResidentUserId");
        migrationBuilder.CreateIndex(name: "IX_Roles_Name", table: "Roles", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_SocialPostMetrics_CampaignId", table: "SocialPostMetrics", column: "CampaignId");
        migrationBuilder.CreateIndex(name: "IX_StatusStates_Domain_Code", table: "StatusStates", columns: ["Domain", "Code"], unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserRoles_RoleId", table: "UserRoles", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_Users_ExternalId", table: "Users", column: "ExternalId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_VisitTypes_Code", table: "VisitTypes", column: "Code", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CaseConferences");
        migrationBuilder.DropTable(name: "ContributionAllocations");
        migrationBuilder.DropTable(name: "HomeVisits");
        migrationBuilder.DropTable(name: "InterventionPlans");
        migrationBuilder.DropTable(name: "OutcomeSnapshots");
        migrationBuilder.DropTable(name: "ProcessRecordings");
        migrationBuilder.DropTable(name: "ResidentAssessments");
        migrationBuilder.DropTable(name: "SocialPostMetrics");
        migrationBuilder.DropTable(name: "UserRoles");
        migrationBuilder.DropTable(name: "Contributions");
        migrationBuilder.DropTable(name: "ResidentCases");
        migrationBuilder.DropTable(name: "VisitTypes");
        migrationBuilder.DropTable(name: "Roles");
        migrationBuilder.DropTable(name: "Campaigns");
        migrationBuilder.DropTable(name: "ContributionTypes");
        migrationBuilder.DropTable(name: "Donors");
        migrationBuilder.DropTable(name: "CaseSubcategories");
        migrationBuilder.DropTable(name: "Safehouses");
        migrationBuilder.DropTable(name: "StatusStates");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "CaseCategories");
    }
}
