using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <inheritdoc />
    public partial class TwoCompetencyTypesAndEmployeeEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "education",
                table: "employees",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("UPDATE matrix_versions SET content = replace(content::text, '\"type\": \"Profile\"', '\"type\": \"Additional\"')::jsonb WHERE content::text LIKE '%\"type\": \"Profile\"%'");
            migrationBuilder.Sql("UPDATE matrix_versions SET snapshot = replace(snapshot::text, '\"type\": \"Profile\"', '\"type\": \"Additional\"')::jsonb WHERE snapshot::text LIKE '%\"type\": \"Profile\"%'");
            migrationBuilder.Sql("UPDATE matrix_version_variants SET content = replace(content::text, '\"type\": \"Profile\"', '\"type\": \"Additional\"')::jsonb WHERE content::text LIKE '%\"type\": \"Profile\"%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "education",
                table: "employees");
        }
    }
}
