using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Adds the step the matrix builder resumes a version at. Existing versions keep null, as versions made outside the builder.
    /// </summary>
    public partial class MatrixBuilderStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "builder_step",
                table: "matrix_versions",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "builder_step",
                table: "matrix_versions");
        }
    }
}
