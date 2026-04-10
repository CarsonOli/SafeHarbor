using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeHarbor.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeVisitDetailsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    target_schema text;
                    target_table text;
                BEGIN
                    -- NOTE: Some deployments still use legacy relation names/schemas.
                    -- Add columns only when a compatible home-visits table exists.
                    SELECT table_schema, table_name
                    INTO target_schema, target_table
                    FROM information_schema.tables
                    WHERE (table_schema, table_name) IN (
                        ('lighthouse', 'home_visits'),
                        ('lighthouse', 'HomeVisits'),
                        ('public', 'home_visits'),
                        ('public', 'HomeVisits')
                    )
                    ORDER BY CASE
                        WHEN table_schema = 'lighthouse' AND table_name = 'home_visits' THEN 1
                        WHEN table_schema = 'lighthouse' AND table_name = 'HomeVisits' THEN 2
                        WHEN table_schema = 'public' AND table_name = 'home_visits' THEN 3
                        ELSE 4
                    END
                    LIMIT 1;

                    IF target_schema IS NULL OR target_table IS NULL THEN
                        RAISE NOTICE 'Skipping AddHomeVisitDetailsFields: no home visits table found.';
                        RETURN;
                    END IF;

                    EXECUTE format(
                        'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS family_cooperation_level text NOT NULL DEFAULT %L;',
                        target_schema, target_table, '');
                    EXECUTE format(
                        'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS follow_up_actions text NOT NULL DEFAULT %L;',
                        target_schema, target_table, '');
                    EXECUTE format(
                        'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS home_environment_observations text NOT NULL DEFAULT %L;',
                        target_schema, target_table, '');
                    EXECUTE format(
                        'ALTER TABLE %I.%I ADD COLUMN IF NOT EXISTS safety_concerns_identified boolean NOT NULL DEFAULT false;',
                        target_schema, target_table);
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    target_schema text;
                    target_table text;
                BEGIN
                    SELECT table_schema, table_name
                    INTO target_schema, target_table
                    FROM information_schema.tables
                    WHERE (table_schema, table_name) IN (
                        ('lighthouse', 'home_visits'),
                        ('lighthouse', 'HomeVisits'),
                        ('public', 'home_visits'),
                        ('public', 'HomeVisits')
                    )
                    ORDER BY CASE
                        WHEN table_schema = 'lighthouse' AND table_name = 'home_visits' THEN 1
                        WHEN table_schema = 'lighthouse' AND table_name = 'HomeVisits' THEN 2
                        WHEN table_schema = 'public' AND table_name = 'home_visits' THEN 3
                        ELSE 4
                    END
                    LIMIT 1;

                    IF target_schema IS NULL OR target_table IS NULL THEN
                        RETURN;
                    END IF;

                    EXECUTE format('ALTER TABLE %I.%I DROP COLUMN IF EXISTS family_cooperation_level;', target_schema, target_table);
                    EXECUTE format('ALTER TABLE %I.%I DROP COLUMN IF EXISTS follow_up_actions;', target_schema, target_table);
                    EXECUTE format('ALTER TABLE %I.%I DROP COLUMN IF EXISTS home_environment_observations;', target_schema, target_table);
                    EXECUTE format('ALTER TABLE %I.%I DROP COLUMN IF EXISTS safety_concerns_identified;', target_schema, target_table);
                END $$;
                """);
        }
    }
}
