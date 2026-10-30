using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionStageScheduler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "versions_fixed",
                table: "promotion_waves",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE promotion_waves SET versions_fixed = true WHERE EXISTS (SELECT 1 FROM promotion_wave_matrices WHERE wave_id = promotion_waves.id)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "versions_fixed",
                table: "promotion_waves");
        }
    }
}
