using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations.Website
{
    /// <inheritdoc />
    public partial class RemoveDemoAndSignUpRequestsAddSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "DemoRequests";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "SignUpRequests";""");

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Subscriptions" (
                    "Id" uuid NOT NULL,
                    "SalonName" character varying(200) NOT NULL,
                    "OwnerFirstName" character varying(100) NOT NULL,
                    "OwnerLastName" character varying(100) NOT NULL,
                    "Email" character varying(256) NOT NULL,
                    "PhoneNumber" character varying(50),
                    "Plan" character varying(20) NOT NULL,
                    "BillingCycle" character varying(20),
                    "TrialEndsAtUtc" timestamp with time zone,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid,
                    "ProvisionedAtUtc" timestamp with time zone,
                    "ProvisionedBy" uuid,
                    "CancelledAtUtc" timestamp with time zone,
                    "CancelledBy" uuid,
                    "CancellationReason" character varying(1000),
                    CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Subscriptions_CreatedAtUtc" ON "Subscriptions" ("CreatedAtUtc");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Subscriptions_Email" ON "Subscriptions" ("Email");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Subscriptions_Plan" ON "Subscriptions" ("Plan");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.CreateTable(
                name: "DemoRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SalonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignUpRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OwnerFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProvisionedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProvisionedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SalonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignUpRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_CreatedAtUtc",
                table: "DemoRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SignUpRequests_CreatedAtUtc",
                table: "SignUpRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SignUpRequests_Email",
                table: "SignUpRequests",
                column: "Email");
        }
    }
}
