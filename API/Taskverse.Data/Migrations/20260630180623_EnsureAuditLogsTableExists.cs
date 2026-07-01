using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAuditLogsTableExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE TABLE IF NOT EXISTS audit_logs
                (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    user_id uuid NOT NULL,
                    action character varying(100) NOT NULL,
                    entity_type character varying(100) NULL,
                    entity_id uuid NULL,
                    details text NULL,
                    occurred_at timestamp with time zone NOT NULL DEFAULT now(),
                    ip_address character varying(45) NULL,
                    CONSTRAINT "PK_audit_logs" PRIMARY KEY (id)
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'fk_audit_logs_user'
                    ) THEN
                        ALTER TABLE audit_logs
                        ADD CONSTRAINT fk_audit_logs_user
                        FOREIGN KEY (user_id)
                        REFERENCES users (id)
                        ON DELETE CASCADE;
                    END IF;
                END
                $$;

                CREATE INDEX IF NOT EXISTS "IX_audit_logs_user_id"
                    ON audit_logs (user_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS audit_logs;
                """);
        }
    }
}
