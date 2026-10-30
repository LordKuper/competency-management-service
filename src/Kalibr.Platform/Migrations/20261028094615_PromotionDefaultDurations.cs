using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionDefaultDurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "promotion_settings",
                keyColumn: "id",
                keyValue: 1,
                column: "stage_durations_days",
                value: new[] { 21, 7, 14, 7, 14, 3, 14 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "promotion_settings",
                keyColumn: "id",
                keyValue: 1,
                column: "stage_durations_days",
                value: new[] { 14, 7, 14, 7, 14, 3, 14 });
        }
    }
}
