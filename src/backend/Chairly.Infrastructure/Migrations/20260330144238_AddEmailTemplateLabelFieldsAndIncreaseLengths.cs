using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplateLabelFieldsAndIncreaseLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "EmailTemplates" ALTER COLUMN "MainMessage" TYPE character varying(5000);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "EmailTemplates" ALTER COLUMN "ClosingMessage" TYPE character varying(3000);
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'DateLabel') THEN
                        ALTER TABLE "EmailTemplates" ADD COLUMN "DateLabel" character varying(200) NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'ServicesLabel') THEN
                        ALTER TABLE "EmailTemplates" ADD COLUMN "ServicesLabel" character varying(200) NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLabel",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "ServicesLabel",
                table: "EmailTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "MainMessage",
                table: "EmailTemplates",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "ClosingMessage",
                table: "EmailTemplates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);
        }
    }
}
