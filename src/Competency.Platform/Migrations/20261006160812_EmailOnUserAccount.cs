using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Moves the e-mail from the employee to the account and removes the personnel number.
    /// Up does not keep the dropped personnel numbers and employee e-mails; Down restores those columns with placeholder values,
    /// does not keep the account e-mails, and fails if a user name is longer than 64 characters.
    /// </summary>
    public partial class EmailOnUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_email_trgm",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_normalized_email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_personnel_number",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_personnel_number_trgm",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "personnel_number",
                table: "employees");

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_user_name",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE users SET
                    email = user_name || '@local.invalid',
                    normalized_email = normalized_user_name || '@LOCAL.INVALID',
                    user_name = user_name || '@local.invalid',
                    normalized_user_name = normalized_user_name || '@LOCAL.INVALID';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE users SET
                    user_name = left(user_name, length(user_name) - length('@local.invalid')),
                    normalized_user_name = left(normalized_user_name, length(normalized_user_name) - length('@LOCAL.INVALID'))
                WHERE normalized_email LIKE '%@LOCAL.INVALID';
                """);

            migrationBuilder.DropIndex(
                name: "IX_users_normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_user_name",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "employees",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personnel_number",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("UPDATE employees SET personnel_number = id::text, email = id::text || '@local.invalid';");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "employees",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "personnel_number",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "employees",
                type: "text",
                nullable: false,
                computedColumnSql: "lower(email)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_email_trgm",
                table: "employees",
                column: "email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_normalized_email",
                table: "employees",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_personnel_number",
                table: "employees",
                column: "personnel_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_personnel_number_trgm",
                table: "employees",
                column: "personnel_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }
    }
}
