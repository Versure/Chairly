using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStaffRoleToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"StaffMembers\" ALTER COLUMN \"Role\" TYPE text USING CASE \"Role\" WHEN 0 THEN 'Manager' WHEN 1 THEN 'StaffMember' END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"StaffMembers\" ALTER COLUMN \"Role\" TYPE integer USING CASE \"Role\" WHEN 'Manager' THEN 0 WHEN 'StaffMember' THEN 1 END;");
        }
    }
}
