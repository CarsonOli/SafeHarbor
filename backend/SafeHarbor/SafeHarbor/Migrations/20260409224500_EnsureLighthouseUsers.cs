using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Ensures the lighthouse.users table exists for local-auth registration in deployed environments.
    /// </summary>
    public partial class EnsureLighthouseUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: We create the schema/table defensively to support existing environments where
            // lighthouse.users is expected but not yet provisioned. The IF NOT EXISTS guards
            // avoid failing deployments that already have the table.
            migrationBuilder.Sql("""
                CREATE SCHEMA IF NOT EXISTS lighthouse;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS lighthouse.users (
                    user_id uuid PRIMARY KEY,
                    f_name text NOT NULL,
                    l_name text NOT NULL,
                    email text NOT NULL,
                    role text NOT NULL,
                    password_hash text NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS ix_lighthouse_users_email
                    ON lighthouse.users (email);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: Down intentionally does not drop lighthouse.users to avoid data loss in shared environments.
        }
    }
}
