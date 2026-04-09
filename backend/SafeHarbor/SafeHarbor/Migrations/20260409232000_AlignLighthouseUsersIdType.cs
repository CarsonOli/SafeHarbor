using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Aligns lighthouse.users.user_id with Guid (uuid) for local-auth registration.
    /// Safe for empty tables; assigns new UUIDs if any rows exist.
    /// </summary>
    public partial class AlignLighthouseUsersIdType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: Ensure uuid generation is available before altering the column type.
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS "pgcrypto";""");

            // NOTE: Only coerce if the column currently exists and is bigint.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'lighthouse'
                          AND table_name = 'users'
                          AND column_name = 'user_id'
                          AND data_type = 'bigint'
                    ) THEN
                        ALTER TABLE lighthouse.users
                            ALTER COLUMN user_id TYPE uuid
                            USING gen_random_uuid();
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: We intentionally avoid converting uuid back to bigint to prevent data loss.
        }
    }
}
