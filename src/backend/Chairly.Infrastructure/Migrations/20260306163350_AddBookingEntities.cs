using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Bookings" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "ClientId" uuid NOT NULL,
                    "StaffMemberId" uuid NOT NULL,
                    "StartTime" timestamp with time zone NOT NULL,
                    "EndTime" timestamp with time zone NOT NULL,
                    "Notes" character varying(1000),
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    "ConfirmedAtUtc" timestamp with time zone,
                    "ConfirmedBy" uuid,
                    "StartedAtUtc" timestamp with time zone,
                    "StartedBy" uuid,
                    "CompletedAtUtc" timestamp with time zone,
                    "CompletedBy" uuid,
                    "CancelledAtUtc" timestamp with time zone,
                    "CancelledBy" uuid,
                    "NoShowAtUtc" timestamp with time zone,
                    "NoShowBy" uuid,
                    CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Bookings_Clients_ClientId" FOREIGN KEY ("ClientId")
                        REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Bookings_StaffMembers_StaffMemberId" FOREIGN KEY ("StaffMemberId")
                        REFERENCES "StaffMembers" ("Id") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "BookingServices" (
                    "Id" uuid NOT NULL,
                    "BookingId" uuid NOT NULL,
                    "ServiceId" uuid NOT NULL,
                    "ServiceName" character varying(200) NOT NULL,
                    "Duration" interval NOT NULL,
                    "Price" numeric(10,2) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    CONSTRAINT "PK_BookingServices" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_BookingServices_Bookings_BookingId" FOREIGN KEY ("BookingId")
                        REFERENCES "Bookings" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Bookings_ClientId" ON "Bookings" ("ClientId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Bookings_StaffMemberId" ON "Bookings" ("StaffMemberId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Bookings_TenantId_StaffMemberId_StartTime" ON "Bookings" ("TenantId", "StaffMemberId", "StartTime");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Bookings_TenantId_StartTime" ON "Bookings" ("TenantId", "StartTime");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_BookingServices_BookingId" ON "BookingServices" ("BookingId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServices");

            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}
