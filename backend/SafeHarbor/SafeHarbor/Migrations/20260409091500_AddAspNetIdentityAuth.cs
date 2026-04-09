using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <summary>
    /// Legacy migration retained for history; intentionally does not create identity tables.
    /// </summary>
    public partial class AddAspNetIdentityAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: This migration is intentionally a no-op.
            // ASP.NET Identity tables (AspNetUsers + related tables) were removed in favor
            // of JWT auth backed by lighthouse.users.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: Intentionally left blank; reverting should not reintroduce AspNet* tables.
        }
    }
}
