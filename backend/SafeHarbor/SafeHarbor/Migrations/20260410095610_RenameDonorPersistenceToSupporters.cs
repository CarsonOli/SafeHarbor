using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Safely aligns legacy donor persistence objects to supporter naming without dropping data.
    /// </summary>
    public partial class RenameDonorPersistenceToSupporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS lighthouse;

                DO $$
                BEGIN
                    -- Keep table-level migration non-destructive: rename in place when legacy names exist.
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'donors'
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'supporters'
                    ) THEN
                        ALTER TABLE lighthouse.donors RENAME TO supporters;
                    END IF;

                    -- Contribution linkage must use supporter_id in both legacy and canonical shapes.
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'contributions'
                    ) THEN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'donor_id'
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions RENAME COLUMN donor_id TO supporter_id;
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'DonorId'
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions RENAME COLUMN "DonorId" TO supporter_id;
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM pg_indexes
                            WHERE schemaname = 'lighthouse' AND tablename = 'contributions' AND indexname = 'IX_Contributions_DonorId'
                        ) THEN
                            ALTER INDEX lighthouse."IX_Contributions_DonorId" RENAME TO "IX_contributions_supporter_id";
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM pg_indexes
                            WHERE schemaname = 'lighthouse' AND tablename = 'contributions' AND indexname = 'IX_contributions_donor_id'
                        ) THEN
                            ALTER INDEX lighthouse."IX_contributions_donor_id" RENAME TO "IX_contributions_supporter_id";
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM pg_constraint
                            WHERE conname = 'FK_Contributions_Donors_DonorId'
                        ) THEN
                            ALTER TABLE lighthouse.contributions DROP CONSTRAINT "FK_Contributions_Donors_DonorId";
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM pg_constraint
                            WHERE conname = 'fk_contributions_donors_donor_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions DROP CONSTRAINT fk_contributions_donors_donor_id;
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.tables
                            WHERE table_schema = 'lighthouse' AND table_name = 'supporters'
                        )
                        AND EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'supporter_id'
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_constraint
                            WHERE conname = 'fk_contributions_supporters_supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions
                            ADD CONSTRAINT fk_contributions_supporters_supporter_id
                            FOREIGN KEY (supporter_id)
                            REFERENCES lighthouse.supporters(id)
                            ON DELETE CASCADE;
                        END IF;
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
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'contributions'
                    ) THEN
                        IF EXISTS (
                            SELECT 1 FROM pg_constraint
                            WHERE conname = 'fk_contributions_supporters_supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions
                            DROP CONSTRAINT fk_contributions_supporters_supporter_id;
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'supporter_id'
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'contributions' AND column_name = 'donor_id'
                        ) THEN
                            ALTER TABLE lighthouse.contributions RENAME COLUMN supporter_id TO donor_id;
                        END IF;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'supporters'
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'donors'
                    ) THEN
                        ALTER TABLE lighthouse.supporters RENAME TO donors;
                    END IF;
                END $$;
                """);
        }
    }
}
