using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrgUnitValidity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_org_units_valid_period",
                table: "org_units");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "org_units");

            migrationBuilder.DropColumn(
                name: "valid_to",
                table: "org_units");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_from",
                table: "org_units",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_to",
                table: "org_units",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_org_units_valid_period",
                table: "org_units",
                sql: "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
        }
    }
}
