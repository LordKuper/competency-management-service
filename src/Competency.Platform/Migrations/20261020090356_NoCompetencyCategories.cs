using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class NoCompetencyCategories : Migration
    {
        /// <summary>
        /// The one category <see cref="Down"/> restores, so the required category columns can come back filled; the categories themselves are gone.
        /// </summary>
        private static readonly Guid PlaceholderCategoryId = new("3f6d2a8e-5b1c-4e7a-9c0d-8e4b2f1a6c35");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE matrix_versions v
                SET snapshot = jsonb_set(v.snapshot, '{competencies}', COALESCE((
                    SELECT jsonb_agg((c.item - 'categoryId' - 'categoryName') ORDER BY c.position)
                    FROM jsonb_array_elements(v.snapshot -> 'competencies') WITH ORDINALITY AS c(item, position)), '[]'::jsonb))
                WHERE v.snapshot IS NOT NULL AND jsonb_typeof(v.snapshot -> 'competencies') = 'array';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_competencies_competency_categories_category_id",
                table: "competencies");

            migrationBuilder.DropTable(
                name: "competency_categories");

            migrationBuilder.DropIndex(
                name: "IX_competencies_category_id",
                table: "competencies");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "competency_versions");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "competencies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_categories", x => x.id);
                    table.CheckConstraint("ck_competency_categories_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_competency_categories_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO competency_categories (id, version, created_at, name, status, author_id)
                VALUES ('3f6d2a8e-5b1c-4e7a-9c0d-8e4b2f1a6c35', 0, now(), 'Без категории', 'Active', NULL);
                """);

            foreach (var table in new[] { "competencies", "competency_versions" })
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "category_id",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    defaultValue: PlaceholderCategoryId);
                migrationBuilder.AlterColumn<Guid>(
                    name: "category_id",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldDefaultValue: PlaceholderCategoryId);
            }

            migrationBuilder.CreateIndex(
                name: "IX_competencies_category_id",
                table: "competencies",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_categories_author_id",
                table: "competency_categories",
                column: "author_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competencies_competency_categories_category_id",
                table: "competencies",
                column: "category_id",
                principalTable: "competency_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
