using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CareerCatalogues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "career_directions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_directions", x => x.id);
                    table.CheckConstraint("ck_career_directions_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_career_directions_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_directions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_categories", x => x.id);
                    table.CheckConstraint("ck_competency_categories_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_competency_categories_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_categories_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_scales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_scales", x => x.id);
                    table.CheckConstraint("ck_competency_scales_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_competency_scales_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_scales_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "career_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_tracks", x => x.id);
                    table.CheckConstraint("ck_career_tracks_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_career_tracks_career_directions_direction_id",
                        column: x => x.direction_id,
                        principalTable: "career_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_tracks_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_tracks_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competencies", x => x.id);
                    table.CheckConstraint("ck_competencies_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.CheckConstraint("ck_competencies_type", "type IN ('Critical', 'Profile', 'Additional')");
                    table.ForeignKey(
                        name: "FK_competencies_career_directions_direction_id",
                        column: x => x.direction_id,
                        principalTable: "career_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competencies_competency_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "competency_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competencies_competency_scales_scale_id",
                        column: x => x.scale_id,
                        principalTable: "competency_scales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competencies_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competencies_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "career_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    role_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    previous_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    next_level_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    salary_from = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    salary_to = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_levels", x => x.id);
                    table.CheckConstraint("ck_career_levels_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_career_levels_career_levels_previous_level_id",
                        column: x => x.previous_level_id,
                        principalTable: "career_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_levels_career_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_levels_org_units_owner_org_unit_id",
                        column: x => x.owner_org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_levels_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_versions",
                columns: table => new
                {
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_versions", x => new { x.competency_id, x.number });
                    table.ForeignKey(
                        name: "FK_competency_versions_competencies_competency_id",
                        column: x => x.competency_id,
                        principalTable: "competencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_versions_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_career_directions_author_id",
                table: "career_directions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_directions_owner_org_unit_id",
                table: "career_directions",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_author_id",
                table: "career_levels",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_owner_org_unit_id",
                table: "career_levels",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_previous_level_id",
                table: "career_levels",
                column: "previous_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_track_id",
                table: "career_levels",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_tracks_author_id",
                table: "career_tracks",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_tracks_direction_id",
                table: "career_tracks",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_tracks_owner_org_unit_id",
                table: "career_tracks",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_author_id",
                table: "competencies",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_category_id",
                table: "competencies",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_direction_id",
                table: "competencies",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_owner_org_unit_id",
                table: "competencies",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_scale_id",
                table: "competencies",
                column: "scale_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_categories_author_id",
                table: "competency_categories",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_categories_owner_org_unit_id",
                table: "competency_categories",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_scales_author_id",
                table: "competency_scales",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_scales_owner_org_unit_id",
                table: "competency_scales",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_versions_changed_by_id",
                table: "competency_versions",
                column: "changed_by_id");

            migrationBuilder.Sql("""
                INSERT INTO competency_categories (id, version, created_at, name, owner_org_unit_id, status, author_id) VALUES
                    ('7d5c4337-0dca-4cbe-8b11-c1435a639c61', 0, now(), 'Масштаб и самостоятельность', NULL, 'Active', NULL),
                    ('a8d728f6-a391-4600-909f-48aa7ce0da7a', 0, now(), 'Профессиональное мастерство', NULL, 'Active', NULL),
                    ('199ccbce-c636-420a-9a44-388d44eea0db', 0, now(), 'Проектирование', NULL, 'Active', NULL),
                    ('8cd2f281-219a-4488-824c-ec0fc4910198', 0, now(), 'Качество', NULL, 'Active', NULL),
                    ('95030b08-a463-498b-a72a-991db2bcb7a3', 0, now(), 'Надёжность', NULL, 'Active', NULL),
                    ('567f9765-2b4c-4aa8-86eb-3876e68cf64c', 0, now(), 'Безопасность', NULL, 'Active', NULL),
                    ('2496dae2-fb2b-467b-b1d1-07f093d91aec', 0, now(), 'Результат и ответственность', NULL, 'Active', NULL),
                    ('af82cc2f-7066-4321-bc0f-df6b771b554f', 0, now(), 'Взаимодействие', NULL, 'Active', NULL),
                    ('7233648d-f9dd-4498-b037-c9c5ace8fb9a', 0, now(), 'Предметная область', NULL, 'Active', NULL),
                    ('c5c473ec-f729-4bd7-89d0-a125a7988bf9', 0, now(), 'Развитие команды', NULL, 'Active', NULL),
                    ('9e5579ac-872f-4b22-9b1b-b9d7d47c11c4', 0, now(), 'Управление', NULL, 'Active', NULL),
                    ('bc7d48e7-5792-4585-bb04-f4e59e06ce3c', 0, now(), 'Профессиональные технологии', NULL, 'Active', NULL);
                """);

            migrationBuilder.Sql("""
                INSERT INTO competency_scales (id, points, version, created_at, name, owner_org_unit_id, status, author_id) VALUES
                    ('3004a734-475a-4e4c-b6a6-18275f5a1d5c',
                     '[{"score":0,"label":"Компетенция не подтверждена"},{"score":1,"label":"Выполняет работу преимущественно с поддержкой"},{"score":2,"label":"Самостоятельно решает типовые задачи"},{"score":3,"label":"Самостоятельно решает сложные и неопределённые задачи, способен помогать другим"},{"score":4,"label":"Формирует подходы и стандарты, решает наиболее сложные задачи и развивает других"}]',
                     0, now(), 'Базовая шкала', NULL, 'Active', NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "career_levels");

            migrationBuilder.DropTable(
                name: "competency_versions");

            migrationBuilder.DropTable(
                name: "career_tracks");

            migrationBuilder.DropTable(
                name: "competencies");

            migrationBuilder.DropTable(
                name: "career_directions");

            migrationBuilder.DropTable(
                name: "competency_categories");

            migrationBuilder.DropTable(
                name: "competency_scales");
        }
    }
}
