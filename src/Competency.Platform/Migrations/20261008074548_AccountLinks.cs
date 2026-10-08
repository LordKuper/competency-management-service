using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Adds the one e-mailed link an account may hold: the SHA-256 of its token, unique among accounts that have one, its expiry and issue time,
    /// and the security stamp it was issued under. Up changes no data: existing accounts keep their passwords and have no link.
    /// Down drops every outstanding link, so an invited account can then get a password only through an administrator's password reset.
    /// </summary>
    public partial class AccountLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "link_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "link_issued_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link_security_stamp",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "link_token_hash",
                table: "users",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_link_token_hash",
                table: "users",
                column: "link_token_hash",
                unique: true,
                filter: "link_token_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_link_token_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "link_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "link_issued_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "link_security_stamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "link_token_hash",
                table: "users");
        }
    }
}
