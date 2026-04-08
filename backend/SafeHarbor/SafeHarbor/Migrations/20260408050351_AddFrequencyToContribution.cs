using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SafeHarbor.Migrations
{
    /// <inheritdoc />
    public partial class AddFrequencyToContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContributionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    LifetimeDonations = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentToken = table.Column<string>(type: "text", nullable: true),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Residents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    MedicalNotes = table.Column<string>(type: "text", nullable: false),
                    CaseWorkerEmail = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Safehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Safehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Domain = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseSubcategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSubcategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseSubcategory_CaseCategory_CaseCategoryId",
                        column: x => x.CaseCategoryId,
                        principalTable: "CaseCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    GoalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserProfileId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResidentCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SafehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaseCategoryId = table.Column<int>(type: "integer", nullable: false),
                    CaseSubcategoryId = table.Column<int>(type: "integer", nullable: true),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidentCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResidentCases_CaseCategory_CaseCategoryId",
                        column: x => x.CaseCategoryId,
                        principalTable: "CaseCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResidentCases_CaseSubcategory_CaseSubcategoryId",
                        column: x => x.CaseSubcategoryId,
                        principalTable: "CaseSubcategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResidentCases_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResidentCases_Safehouses_SafehouseId",
                        column: x => x.SafehouseId,
                        principalTable: "Safehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResidentCases_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DonorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContributionTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    ContributionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contributions_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contributions_ContributionType_ContributionTypeId",
                        column: x => x.ContributionTypeId,
                        principalTable: "ContributionType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_Donors_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Donors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialPostMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Reach = table.Column<int>(type: "integer", nullable: false),
                    Engagements = table.Column<int>(type: "integer", nullable: false),
                    AttributedDonationAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AttributedDonationCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPostMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialPostMetrics_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CaseConferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConferenceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    OutcomeSummary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseConferences_ResidentCases_ResidentCaseId",
                        column: x => x.ResidentCaseId,
                        principalTable: "ResidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseConferences_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HomeVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    VisitDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeVisits_ResidentCases_ResidentCaseId",
                        column: x => x.ResidentCaseId,
                        principalTable: "ResidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomeVisits_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HomeVisits_VisitType_VisitTypeId",
                        column: x => x.VisitTypeId,
                        principalTable: "VisitType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterventionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    PlanDetails = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterventionPlans_ResidentCases_ResidentCaseId",
                        column: x => x.ResidentCaseId,
                        principalTable: "ResidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterventionPlans_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcessRecordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessRecordings_ResidentCases_ResidentCaseId",
                        column: x => x.ResidentCaseId,
                        principalTable: "ResidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResidentAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusStateId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidentAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResidentAssessments_ResidentCases_ResidentCaseId",
                        column: x => x.ResidentCaseId,
                        principalTable: "ResidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResidentAssessments_StatusState_StatusStateId",
                        column: x => x.StatusStateId,
                        principalTable: "StatusState",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContributionAllocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SafehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountAllocated = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionAllocation_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContributionAllocation_Safehouses_SafehouseId",
                        column: x => x.SafehouseId,
                        principalTable: "Safehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1c3e5d7-2f4b-4e6a-8d0c-2b4a6f8d0e2c"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system", null, "Staff", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d2b2f671-5b1a-4a2b-8c2e-4b6a8f1d2c34"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system", null, "Admin", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "StatusState",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "Domain", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system", null, 0, "Active", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system", null, 0, "Pending", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system", null, 0, "Closed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StatusStateId",
                table: "Campaigns",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConferences_ResidentCaseId",
                table: "CaseConferences",
                column: "ResidentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConferences_StatusStateId",
                table: "CaseConferences",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseSubcategory_CaseCategoryId",
                table: "CaseSubcategory",
                column: "CaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionAllocation_ContributionId",
                table: "ContributionAllocation",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionAllocation_SafehouseId",
                table: "ContributionAllocation",
                column: "SafehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_CampaignId",
                table: "Contributions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_ContributionTypeId",
                table: "Contributions",
                column: "ContributionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_DonorId",
                table: "Contributions",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_StatusStateId",
                table: "Contributions",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_ResidentCaseId",
                table: "HomeVisits",
                column: "ResidentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_StatusStateId",
                table: "HomeVisits",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_VisitTypeId",
                table: "HomeVisits",
                column: "VisitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionPlans_ResidentCaseId",
                table: "InterventionPlans",
                column: "ResidentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionPlans_StatusStateId",
                table: "InterventionPlans",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRecordings_ResidentCaseId",
                table: "ProcessRecordings",
                column: "ResidentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentAssessments_ResidentCaseId",
                table: "ResidentAssessments",
                column: "ResidentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentAssessments_StatusStateId",
                table: "ResidentAssessments",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentCases_CaseCategoryId",
                table: "ResidentCases",
                column: "CaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentCases_CaseSubcategoryId",
                table: "ResidentCases",
                column: "CaseSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentCases_ResidentId",
                table: "ResidentCases",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentCases_SafehouseId",
                table: "ResidentCases",
                column: "SafehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentCases_StatusStateId",
                table: "ResidentCases",
                column: "StatusStateId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialPostMetrics_CampaignId",
                table: "SocialPostMetrics",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseConferences");

            migrationBuilder.DropTable(
                name: "ContributionAllocation");

            migrationBuilder.DropTable(
                name: "HomeVisits");

            migrationBuilder.DropTable(
                name: "InterventionPlans");

            migrationBuilder.DropTable(
                name: "ProcessRecordings");

            migrationBuilder.DropTable(
                name: "ResidentAssessments");

            migrationBuilder.DropTable(
                name: "SocialPostMetrics");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Contributions");

            migrationBuilder.DropTable(
                name: "VisitType");

            migrationBuilder.DropTable(
                name: "ResidentCases");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "ContributionType");

            migrationBuilder.DropTable(
                name: "Donors");

            migrationBuilder.DropTable(
                name: "CaseSubcategory");

            migrationBuilder.DropTable(
                name: "Residents");

            migrationBuilder.DropTable(
                name: "Safehouses");

            migrationBuilder.DropTable(
                name: "StatusState");

            migrationBuilder.DropTable(
                name: "CaseCategory");
        }
    }
}
