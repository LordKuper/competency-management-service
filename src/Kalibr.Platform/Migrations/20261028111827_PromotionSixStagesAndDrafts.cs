using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionSixStagesAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_waves_ends_on",
                table: "promotion_waves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_waves_processed_stage",
                table: "promotion_waves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_settings_durations",
                table: "promotion_settings");

            migrationBuilder.Sql(
                "UPDATE promotion_waves SET ends_on = ARRAY[ends_on[1], ends_on[2], ends_on[3], ends_on[4], ends_on[5], ends_on[7]], " +
                "processed_stage = CASE WHEN processed_stage >= 6 THEN processed_stage - 1 ELSE processed_stage END " +
                "WHERE cardinality(ends_on) = 7");

            migrationBuilder.DropIndex(
                name: "ux_promotion_applications_unfinished",
                table: "promotion_applications");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "promotion_raters",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_confirmed_at",
                table: "promotion_raters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "email_confirmed_by",
                table: "promotion_raters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "review_letter_pending",
                table: "promotion_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "promotion_settings",
                keyColumn: "id",
                keyValue: 1,
                column: "stage_durations_days",
                value: new[] { 10, 2, 5, 2, 5, 5 });

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_waves_ends_on",
                table: "promotion_waves",
                sql: "cardinality(ends_on) = 6");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_waves_processed_stage",
                table: "promotion_waves",
                sql: "processed_stage BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_settings_durations",
                table: "promotion_settings",
                sql: "cardinality(stage_durations_days) = 6");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_raters_email_confirmed_by",
                table: "promotion_raters",
                column: "email_confirmed_by");

            migrationBuilder.CreateIndex(
                name: "ux_promotion_applications_unfinished",
                table: "promotion_applications",
                columns: new[] { "wave_id", "employee_id" },
                unique: true,
                filter: "status IN ('Draft', 'Submitted', 'Admitted', 'UnderReview')");

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_raters_users_email_confirmed_by",
                table: "promotion_raters",
                column: "email_confirmed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_raters_users_email_confirmed_by",
                table: "promotion_raters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_waves_ends_on",
                table: "promotion_waves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_waves_processed_stage",
                table: "promotion_waves");

            migrationBuilder.DropCheckConstraint(
                name: "ck_promotion_settings_durations",
                table: "promotion_settings");

            migrationBuilder.DropIndex(
                name: "IX_promotion_raters_email_confirmed_by",
                table: "promotion_raters");

            migrationBuilder.DropIndex(
                name: "ux_promotion_applications_unfinished",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "email",
                table: "promotion_raters");

            migrationBuilder.DropColumn(
                name: "email_confirmed_at",
                table: "promotion_raters");

            migrationBuilder.DropColumn(
                name: "email_confirmed_by",
                table: "promotion_raters");

            migrationBuilder.DropColumn(
                name: "review_letter_pending",
                table: "promotion_applications");

            migrationBuilder.Sql(
                "UPDATE promotion_waves SET ends_on = ARRAY[ends_on[1], ends_on[2], ends_on[3], ends_on[4], ends_on[5], ends_on[5], ends_on[6]], " +
                "processed_stage = CASE WHEN processed_stage >= 5 THEN processed_stage + 1 ELSE processed_stage END " +
                "WHERE cardinality(ends_on) = 6");

            migrationBuilder.UpdateData(
                table: "promotion_settings",
                keyColumn: "id",
                keyValue: 1,
                column: "stage_durations_days",
                value: new[] { 21, 7, 14, 7, 14, 3, 14 });

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_waves_ends_on",
                table: "promotion_waves",
                sql: "cardinality(ends_on) = 7");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_waves_processed_stage",
                table: "promotion_waves",
                sql: "processed_stage BETWEEN 0 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promotion_settings_durations",
                table: "promotion_settings",
                sql: "cardinality(stage_durations_days) = 7");

            migrationBuilder.CreateIndex(
                name: "ux_promotion_applications_unfinished",
                table: "promotion_applications",
                columns: new[] { "wave_id", "employee_id" },
                unique: true,
                filter: "status IN ('Submitted', 'Admitted', 'UnderReview')");
        }
    }
}
