using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Defensively adds columns that ExpandProcessRecordingFields missed in deployed environments
    /// because that migration used unqualified table names (public schema) instead of lighthouse.process_recordings.
    /// Uses IF NOT EXISTS guards so this is safe to run on databases where the columns already exist.
    /// </summary>
    public partial class EnsureProcessRecordingColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='social_worker') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN social_worker text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='session_type') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN session_type text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='session_duration_minutes') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN session_duration_minutes integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='emotional_state_observed') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN emotional_state_observed text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='emotional_state_end') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN emotional_state_end text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='interventions_applied') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN interventions_applied text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='follow_up_actions') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN follow_up_actions text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='progress_noted') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN progress_noted boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='concerns_flagged') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN concerns_flagged boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='referral_made') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN referral_made boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='lighthouse' AND table_name='process_recordings' AND column_name='notes_restricted') THEN
                        ALTER TABLE lighthouse.process_recordings ADD COLUMN notes_restricted text NULL;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op to avoid data loss.
        }
    }
}
