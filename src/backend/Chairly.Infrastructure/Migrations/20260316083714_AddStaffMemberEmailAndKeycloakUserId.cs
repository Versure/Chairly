using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMemberEmailAndKeycloakUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'StaffMembers' AND column_name = 'Email'
                    ) THEN
                        ALTER TABLE "StaffMembers" ADD COLUMN "Email" character varying(256) NOT NULL DEFAULT '';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'StaffMembers' AND column_name = 'KeycloakUserId'
                    ) THEN
                        ALTER TABLE "StaffMembers" ADD COLUMN "KeycloakUserId" character varying(256) NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StaffMembers_TenantId_Email"
                    ON "StaffMembers" ("TenantId", "Email");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_TenantId_Email",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "KeycloakUserId",
                table: "StaffMembers");
        }
    }
}
