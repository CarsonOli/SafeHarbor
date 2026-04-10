using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Links auth users to supporter CRM profiles so "Your Donations" can be scoped server-side.
    /// </summary>
    public partial class AddUsersSupporterLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS lighthouse;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'users'
                    ) THEN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'users' AND column_name = 'supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.users
                            ADD COLUMN supporter_id bigint;
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.tables
                            WHERE table_schema = 'lighthouse' AND table_name = 'supporters'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.table_constraints
                            WHERE table_schema = 'lighthouse'
                              AND table_name = 'users'
                              AND constraint_name = 'fk_users_supporter'
                        ) THEN
                            ALTER TABLE lighthouse.users
                            ADD CONSTRAINT fk_users_supporter
                            FOREIGN KEY (supporter_id)
                            REFERENCES lighthouse.supporters(supporter_id);
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'uq_users_supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.users
                            ADD CONSTRAINT uq_users_supporter_id UNIQUE (supporter_id);
                        END IF;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'donations'
                    ) THEN
                        CREATE INDEX IF NOT EXISTS idx_donations_supporter_id
                        ON lighthouse.donations(supporter_id);
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'users'
                    ) THEN
                        CREATE INDEX IF NOT EXISTS idx_users_supporter_id
                        ON lighthouse.users(supporter_id);
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
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'lighthouse' AND table_name = 'users'
                    ) THEN
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.table_constraints
                            WHERE table_schema = 'lighthouse'
                              AND table_name = 'users'
                              AND constraint_name = 'fk_users_supporter'
                        ) THEN
                            ALTER TABLE lighthouse.users DROP CONSTRAINT fk_users_supporter;
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.table_constraints
                            WHERE table_schema = 'lighthouse'
                              AND table_name = 'users'
                              AND constraint_name = 'uq_users_supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.users DROP CONSTRAINT uq_users_supporter_id;
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'lighthouse' AND table_name = 'users' AND column_name = 'supporter_id'
                        ) THEN
                            ALTER TABLE lighthouse.users DROP COLUMN supporter_id;
                        END IF;
                    END IF;

                    DROP INDEX IF EXISTS lighthouse.idx_users_supporter_id;
                    DROP INDEX IF EXISTS lighthouse.idx_donations_supporter_id;
                END $$;
                """);
        }
    }
}
