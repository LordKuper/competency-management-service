using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Replaces the employee's free-text position with a reference to the new position catalogue and gives units their lists of allowed positions.
    /// Up trims every position of the whitespace .NET <c>string.Trim()</c> removes (a blank one becomes <c>-</c>), merges the names that differ only
    /// in letter case into one catalogue entry spelled as most employees spell it (on a tie, as the earliest employee does), allows each position
    /// in every unit whose employees, working or not and in active units or not, hold it, and rebuilds the employee search index without the position.
    /// No audit events are written for the moved data. Down restores the text from the catalogue name, so merged spellings come back as the chosen one.
    /// </summary>
    public partial class PositionCatalogue : Migration
    {
        private const string Whitespace = @"U&' \0009\000A\000B\000C\000D\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "russian")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "name" }),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                });

            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_positions_name_lower" ON positions (lower(name));""");

            migrationBuilder.CreateIndex(
                name: "IX_positions_search_vector",
                table: "positions",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateTable(
                name: "org_unit_positions",
                columns: table => new
                {
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_unit_positions", x => new { x.org_unit_id, x.position_id });
                    table.ForeignKey(
                        name: "FK_org_unit_positions_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_org_unit_positions_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_positions_position_id",
                table: "org_unit_positions",
                column: "position_id");

            migrationBuilder.AddColumn<Guid>(
                name: "position_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                $"""
                UPDATE employees SET position = coalesce(nullif(btrim(position, {Whitespace}), ''), '-');

                INSERT INTO positions (id, name, is_active, version, created_at)
                SELECT gen_random_uuid(), spelling.position, true, 0, now()
                FROM (
                    SELECT DISTINCT ON (lower(position)) position
                    FROM employees
                    GROUP BY position
                    ORDER BY lower(position), count(*) DESC, min(created_at), position
                ) AS spelling;

                UPDATE employees AS employee SET position_id = catalogue.id
                FROM positions AS catalogue
                WHERE lower(catalogue.name) = lower(employee.position);

                INSERT INTO org_unit_positions (org_unit_id, position_id)
                SELECT DISTINCT org_unit_id, position_id FROM employees;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "position_id",
                table: "employees",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_position_id",
                table: "employees",
                column: "position_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_positions_position_id",
                table: "employees",
                column: "position_id",
                principalTable: "positions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropIndex(
                name: "IX_employees_search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "position",
                table: "employees");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "employees",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "last_name", "first_name", "middle_name" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_search_vector",
                table: "employees",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "position",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE employees AS employee SET position = catalogue.name
                FROM positions AS catalogue
                WHERE catalogue.id = employee.position_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_employees_search_vector",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "employees");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "employees",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "last_name", "first_name", "middle_name", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_search_vector",
                table: "employees",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_positions_position_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_position_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "position_id",
                table: "employees");

            migrationBuilder.DropTable(
                name: "org_unit_positions");

            migrationBuilder.DropTable(
                name: "positions");
        }
    }
}
