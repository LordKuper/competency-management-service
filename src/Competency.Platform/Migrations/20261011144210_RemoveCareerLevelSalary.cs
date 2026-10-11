using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Drops the salary range of career levels: the system keeps no financial information. Down restores the columns empty.
    /// </summary>
    public partial class RemoveCareerLevelSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "salary_from",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "salary_to",
                table: "career_levels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "salary_from",
                table: "career_levels",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "salary_to",
                table: "career_levels",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }
    }
}
