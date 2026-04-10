using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Ensures the expanded process recording fields exist in canonical snake_case and reconciles
    /// previously-applied PascalCase column names produced by legacy EF table mappings.
    /// </summary>
    public partial class EnsureProcessRecordingColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS lighthouse;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'SocialWorker'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "SocialWorker" TO social_worker;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'SessionType'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "SessionType" TO session_type;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'SessionDurationMinutes'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "SessionDurationMinutes" TO session_duration_minutes;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'EmotionalStateObserved'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "EmotionalStateObserved" TO emotional_state_observed;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'EmotionalStateEnd'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "EmotionalStateEnd" TO emotional_state_end;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'InterventionsApplied'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "InterventionsApplied" TO interventions_applied;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'FollowUpActions'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "FollowUpActions" TO follow_up_actions;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'ProgressNoted'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "ProgressNoted" TO progress_noted;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'ConcernsFlagged'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "ConcernsFlagged" TO concerns_flagged;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'ReferralMade'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "ReferralMade" TO referral_made;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'lighthouse' AND table_name = 'process_recordings' AND column_name = 'NotesRestricted'
                    ) THEN
                        ALTER TABLE lighthouse.process_recordings RENAME COLUMN "NotesRestricted" TO notes_restricted;
                    END IF;

                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS social_worker text NOT NULL DEFAULT '';
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS session_type text NOT NULL DEFAULT '';
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS session_duration_minutes integer NULL;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS emotional_state_observed text NOT NULL DEFAULT '';
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS emotional_state_end text NULL;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS interventions_applied text NULL;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS follow_up_actions text NULL;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS progress_noted boolean NOT NULL DEFAULT false;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS concerns_flagged boolean NOT NULL DEFAULT false;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS referral_made boolean NOT NULL DEFAULT false;
                    ALTER TABLE lighthouse.process_recordings ADD COLUMN IF NOT EXISTS notes_restricted text NULL;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    -- NOTE: Down intentionally keeps additive reconciliation columns intact to avoid
                    -- destructive rollbacks in shared environments where API writes now depend on them.
                    RAISE NOTICE 'EnsureProcessRecordingColumns down script is intentionally no-op.';
                END $$;
                """);
        }
    }
}
