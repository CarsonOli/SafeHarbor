using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Reconciles historical EF naming with canonical lighthouse snake_case names.
    /// </summary>
    public partial class ReconcileCanonicalLighthouseMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS lighthouse;

                DO $$
                DECLARE
                    rec record;
                BEGIN
                    -- Move legacy public tables into the canonical lighthouse schema when present.
                    FOR rec IN
                        SELECT *
                        FROM (VALUES
                            ('Campaigns', 'campaigns'),
                            ('CaseConferences', 'case_conferences'),
                            ('Contributions', 'contributions'),
                            ('ContributionAllocations', 'contribution_allocations'),
                            ('Donors', 'donors'),
                            ('HomeVisits', 'home_visits'),
                            ('InterventionPlans', 'intervention_plans'),
                            ('ProcessRecordings', 'process_recordings'),
                            ('ResidentAssessments', 'resident_assessments'),
                            ('ResidentCases', 'resident_cases'),
                            ('Residents', 'residents'),
                            ('Safehouses', 'safehouses'),
                            ('SocialPostMetrics', 'social_post_metrics'),
                            ('StatusState', 'status_state'),
                            ('CaseCategory', 'case_category'),
                            ('CaseSubcategory', 'case_subcategory'),
                            ('ContributionType', 'contribution_type'),
                            ('VisitType', 'visit_type'),
                            ('Roles', 'roles'),
                            ('UserProfiles', 'user_profiles'),
                            ('UserRoles', 'user_roles')
                        ) AS t(source_name, canonical_name)
                    LOOP
                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables
                            WHERE table_schema = 'public' AND table_name = rec.source_name
                        ) THEN
                            EXECUTE format('ALTER TABLE public.%I SET SCHEMA lighthouse', rec.source_name);
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables
                            WHERE table_schema = 'lighthouse' AND table_name = rec.source_name
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.tables
                            WHERE table_schema = 'lighthouse' AND table_name = rec.canonical_name
                        ) THEN
                            EXECUTE format('ALTER TABLE lighthouse.%I RENAME TO %I', rec.source_name, rec.canonical_name);
                        END IF;
                    END LOOP;

                    -- Column rename guards for the highest-traffic logged-in entities.
                    PERFORM 1;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='donors' AND column_name='DisplayName') THEN
                        ALTER TABLE lighthouse.donors RENAME COLUMN "DisplayName" TO display_name;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='donors' AND column_name='LifetimeDonations') THEN
                        ALTER TABLE lighthouse.donors RENAME COLUMN "LifetimeDonations" TO lifetime_donations;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='donors' AND column_name='LastActivityAt') THEN
                        ALTER TABLE lighthouse.donors RENAME COLUMN "LastActivityAt" TO last_activity_at;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='contributions' AND column_name='ContributionDate') THEN
                        ALTER TABLE lighthouse.contributions RENAME COLUMN "ContributionDate" TO contribution_date;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='contributions' AND column_name='DonorId') THEN
                        ALTER TABLE lighthouse.contributions RENAME COLUMN "DonorId" TO donor_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='contributions' AND column_name='CampaignId') THEN
                        ALTER TABLE lighthouse.contributions RENAME COLUMN "CampaignId" TO campaign_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='contributions' AND column_name='ContributionTypeId') THEN
                        ALTER TABLE lighthouse.contributions RENAME COLUMN "ContributionTypeId" TO contribution_type_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='contributions' AND column_name='StatusStateId') THEN
                        ALTER TABLE lighthouse.contributions RENAME COLUMN "StatusStateId" TO status_state_id;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='SafehouseId') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "SafehouseId" TO safehouse_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='ResidentId') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "ResidentId" TO resident_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='CaseCategoryId') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "CaseCategoryId" TO case_category_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='CaseSubcategoryId') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "CaseSubcategoryId" TO case_subcategory_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='StatusStateId') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "StatusStateId" TO status_state_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='OpenedAt') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "OpenedAt" TO opened_at;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='resident_cases' AND column_name='ClosedAt') THEN
                        ALTER TABLE lighthouse.resident_cases RENAME COLUMN "ClosedAt" TO closed_at;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='ResidentCaseId') THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "ResidentCaseId" TO resident_case_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='RecordedAt') THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "RecordedAt" TO recorded_at;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='home_visits' AND column_name='ResidentCaseId') THEN
                        ALTER TABLE lighthouse.home_visits RENAME COLUMN "ResidentCaseId" TO resident_case_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='home_visits' AND column_name='VisitTypeId') THEN
                        ALTER TABLE lighthouse.home_visits RENAME COLUMN "VisitTypeId" TO visit_type_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='home_visits' AND column_name='StatusStateId') THEN
                        ALTER TABLE lighthouse.home_visits RENAME COLUMN "StatusStateId" TO status_state_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='home_visits' AND column_name='VisitDate') THEN
                        ALTER TABLE lighthouse.home_visits RENAME COLUMN "VisitDate" TO visit_date;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='case_conferences' AND column_name='ResidentCaseId') THEN
                        ALTER TABLE lighthouse.case_conferences RENAME COLUMN "ResidentCaseId" TO resident_case_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='case_conferences' AND column_name='ConferenceDate') THEN
                        ALTER TABLE lighthouse.case_conferences RENAME COLUMN "ConferenceDate" TO conference_date;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='case_conferences' AND column_name='StatusStateId') THEN
                        ALTER TABLE lighthouse.case_conferences RENAME COLUMN "StatusStateId" TO status_state_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='case_conferences' AND column_name='OutcomeSummary') THEN
                        ALTER TABLE lighthouse.case_conferences RENAME COLUMN "OutcomeSummary" TO outcome_summary;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='social_post_metrics' AND column_name='CampaignId') THEN
                        ALTER TABLE lighthouse.social_post_metrics RENAME COLUMN "CampaignId" TO campaign_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='social_post_metrics' AND column_name='PostedAt') THEN
                        ALTER TABLE lighthouse.social_post_metrics RENAME COLUMN "PostedAt" TO posted_at;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='social_post_metrics' AND column_name='ContentType') THEN
                        ALTER TABLE lighthouse.social_post_metrics RENAME COLUMN "ContentType" TO content_type;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='social_post_metrics' AND column_name='AttributedDonationAmount') THEN
                        ALTER TABLE lighthouse.social_post_metrics RENAME COLUMN "AttributedDonationAmount" TO attributed_donation_amount;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='social_post_metrics' AND column_name='AttributedDonationCount') THEN
                        ALTER TABLE lighthouse.social_post_metrics RENAME COLUMN "AttributedDonationCount" TO attributed_donation_count;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    -- Down migration is intentionally conservative to avoid destructive table/schema rewrites
                    -- in shared deployments where data may already be queried by canonical names.
                    RAISE NOTICE 'ReconcileCanonicalLighthouseMappings down script is intentionally no-op.';
                END $$;
                """);
        }
    }
}
