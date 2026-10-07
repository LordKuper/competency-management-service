using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Splits the employee's full name into last, first and middle name, and makes <c>full_name</c> derived from them.
    /// Up fails, changing nothing, when a name part is longer than 100 characters; Down fails when a full name is longer than 200 characters.
    /// </summary>
    public partial class EmployeeNameInThreeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "middle_name",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE employees AS employee SET
                    last_name = coalesce(parsed.parts[1], '-'),
                    first_name = coalesce(parsed.parts[2], '-'),
                    middle_name = nullif(array_to_string(parsed.parts[3:], ' '), '')
                FROM (
                    SELECT id, array_remove(regexp_split_to_array(full_name, '\s+'), '') AS parts
                    FROM employees
                ) AS parsed
                WHERE parsed.id = employee.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_employees_full_name_trgm",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "employees",
                type: "text",
                nullable: false,
                computedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "employees",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "last_name", "first_name", "middle_name", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_full_name_trgm",
                table: "employees",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_search_vector",
                table: "employees",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_full_name_trgm",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "employees");

            migrationBuilder.Sql(
                """
                ALTER TABLE employees ALTER COLUMN full_name DROP EXPRESSION;
                ALTER TABLE employees ALTER COLUMN full_name TYPE character varying(200);
                """);

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "middle_name",
                table: "employees");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "employees",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "full_name", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_full_name_trgm",
                table: "employees",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_search_vector",
                table: "employees",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
