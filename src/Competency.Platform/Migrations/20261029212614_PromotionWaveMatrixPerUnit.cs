using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionWaveMatrixPerUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_applications_promotion_wave_matrices_wave_id_matr~",
                table: "promotion_applications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_promotion_wave_matrices",
                table: "promotion_wave_matrices");

            migrationBuilder.DropIndex(
                name: "IX_promotion_applications_wave_id_matrix_id",
                table: "promotion_applications");

            migrationBuilder.AddPrimaryKey(
                name: "PK_promotion_wave_matrices",
                table: "promotion_wave_matrices",
                columns: new[] { "wave_id", "org_unit_id", "matrix_id" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_wave_id_org_unit_id_matrix_id",
                table: "promotion_applications",
                columns: new[] { "wave_id", "org_unit_id", "matrix_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_applications_promotion_wave_matrices_wave_id_org_~",
                table: "promotion_applications",
                columns: new[] { "wave_id", "org_unit_id", "matrix_id" },
                principalTable: "promotion_wave_matrices",
                principalColumns: new[] { "wave_id", "org_unit_id", "matrix_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_applications_promotion_wave_matrices_wave_id_org_~",
                table: "promotion_applications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_promotion_wave_matrices",
                table: "promotion_wave_matrices");

            migrationBuilder.DropIndex(
                name: "IX_promotion_applications_wave_id_org_unit_id_matrix_id",
                table: "promotion_applications");

            migrationBuilder.AddPrimaryKey(
                name: "PK_promotion_wave_matrices",
                table: "promotion_wave_matrices",
                columns: new[] { "wave_id", "matrix_id" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_wave_id_matrix_id",
                table: "promotion_applications",
                columns: new[] { "wave_id", "matrix_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_applications_promotion_wave_matrices_wave_id_matr~",
                table: "promotion_applications",
                columns: new[] { "wave_id", "matrix_id" },
                principalTable: "promotion_wave_matrices",
                principalColumns: new[] { "wave_id", "matrix_id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
