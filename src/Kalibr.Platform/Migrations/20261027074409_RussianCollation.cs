using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Orders the name columns the server sorts by with the built-in Russian ICU collation. The generated search vectors of units and positions
    /// are dropped and added back, because PostgreSQL cannot change the type of a column a generated column is built on; the vectors refill on their own.
    /// </summary>
    public partial class RussianCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "org_units");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "positions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "org_units",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "legal_entities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competency_scales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competency_blocks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competencies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_tracks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_levels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_directions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "employees",
                type: "text",
                nullable: false,
                computedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')",
                stored: true,
                collation: "ru-x-icu",
                oldClrType: typeof(string),
                oldType: "text",
                oldComputedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')",
                oldStored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "positions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_search_vector",
                table: "positions",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "org_units",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name" });

            migrationBuilder.CreateIndex(
                name: "IX_org_units_search_vector",
                table: "org_units",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "org_units");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "positions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "org_units",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "legal_entities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competency_scales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competency_blocks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "competencies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_tracks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_levels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "career_directions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ru-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "employees",
                type: "text",
                nullable: false,
                computedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')",
                stored: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldComputedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')",
                oldStored: true,
                oldCollation: "ru-x-icu");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "positions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_search_vector",
                table: "positions",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "org_units",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "russian")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name" });

            migrationBuilder.CreateIndex(
                name: "IX_org_units_search_vector",
                table: "org_units",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
