using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Ensures the auth_accounts table exists for local auth login and registration.
    /// This table is queried via raw SQL in PostgresLocalAccountStore and is not an EF Core entity,
    /// so it must be created explicitly here to survive MigrateAsync() on fresh deployments.
    /// </summary>
    public partial class EnsureAuthAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS auth_accounts (
                    email text NOT NULL,
                    password_hash bytea NOT NULL,
                    role text NOT NULL,
                    CONSTRAINT pk_auth_accounts PRIMARY KEY (email, role)
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op to avoid data loss in shared environments.
        }
    }
}
