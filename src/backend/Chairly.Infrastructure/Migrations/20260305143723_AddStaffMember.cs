using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "StaffMembers" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "FirstName" character varying(100) NOT NULL,
                    "LastName" character varying(100) NOT NULL,
                    "Role" integer NOT NULL,
                    "Color" character varying(20) NOT NULL,
                    "PhotoUrl" character varying(500),
                    "ScheduleJson" text NOT NULL,
                    "DeactivatedAtUtc" timestamp with time zone,
                    "DeactivatedBy" uuid,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    CONSTRAINT "PK_StaffMembers" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_StaffMembers_FirstName_LastName_TenantId" ON "StaffMembers" ("FirstName", "LastName", "TenantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffMembers");
        }
    }
}
