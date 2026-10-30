using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionHeldAtCloseMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "was_held_at_close",
                table: "promotion_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "was_held_at_close",
                table: "promotion_applications");
        }
    }
}
