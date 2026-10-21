using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    request_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "career_directions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_directions", x => x.id);
                    table.CheckConstraint("ck_career_directions_status", "status IN ('Draft', 'Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "career_level_positions",
                columns: table => new
                {
                    level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_level_positions", x => new { x.level_id, x.position_id });
                    table.ForeignKey(
                        name: "FK_career_level_positions_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "career_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_levels", x => x.id);
                    table.CheckConstraint("ck_career_levels_status", "status IN ('Draft', 'Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "career_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "competencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    evidence_examples = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competencies", x => x.id);
                    table.CheckConstraint("ck_competencies_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_competencies_career_directions_direction_id",
                        column: x => x.direction_id,
                        principalTable: "career_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    competency_ids = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_blocks", x => x.id);
                    table.CheckConstraint("ck_competency_blocks_status", "status IN ('Draft', 'Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "competency_matrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    track_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_matrices", x => x.id);
                    table.CheckConstraint("ck_competency_matrices_kind", "(direction_id IS NOT NULL AND track_id IS NOT NULL AND position_id IS NULL) OR (direction_id IS NULL AND track_id IS NULL AND position_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_competency_matrices_career_directions_direction_id",
                        column: x => x.direction_id,
                        principalTable: "career_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_matrices_career_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_matrices_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_scales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<string>(type: "jsonb", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_scales", x => x.id);
                    table.CheckConstraint("ck_competency_scales_status", "status IN ('Draft', 'Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "competency_versions",
                columns: table => new
                {
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    evidence_examples = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: false, computedColumnSql: "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')", stored: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "russian")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "last_name", "first_name", "middle_name" }),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    head_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "russian")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "name" }),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_org_units_employees_head_employee_id",
                        column: x => x.head_employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_org_units_org_units_parent_id",
                        column: x => x.parent_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    is_global_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_hr = table.Column<bool>(type: "boolean", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    link_token_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    link_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    link_issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    link_security_stamp = table.Column<string>(type: "text", nullable: true),
                    user_name = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_user_name = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_unit_hr",
                columns: table => new
                {
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_unit_hr", x => x.org_unit_id);
                    table.ForeignKey(
                        name: "FK_org_unit_hr_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_org_unit_hr_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "matrix_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    step = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    builder_step = table.Column<short>(type: "smallint", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_versions", x => x.id);
                    table.CheckConstraint("ck_matrix_versions_status", "status IN ('Draft', 'OnReview', 'Returned', 'Approved', 'Published', 'Archived')");
                    table.CheckConstraint("ck_matrix_versions_step", "step IN ('Superior', 'Hr')");
                    table.ForeignKey(
                        name: "FK_matrix_versions_competency_matrices_matrix_id",
                        column: x => x.matrix_id,
                        principalTable: "competency_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_versions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_version_decisions",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    step = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    variant_number = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_version_decisions", x => new { x.version_id, x.sequence });
                    table.CheckConstraint("ck_matrix_version_decisions_kind", "kind IN ('Submitted', 'Skipped', 'Approved', 'AutoApproved', 'Returned', 'AcceptedChanges', 'AcceptedAndRevising', 'RolledBack', 'Published')");
                    table.CheckConstraint("ck_matrix_version_decisions_step", "step IN ('Superior', 'Hr')");
                    table.ForeignKey(
                        name: "FK_matrix_version_decisions_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_version_decisions_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_version_variants",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_version_variants", x => new { x.version_id, x.number });
                    table.CheckConstraint("ck_matrix_version_variants_kind", "kind IN ('Author', 'Approver')");
                    table.ForeignKey(
                        name: "FK_matrix_version_variants_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_version_variants_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_actor_timestamp",
                table: "audit_events",
                columns: new[] { "actor", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_entity_type_entity_id_timestamp",
                table: "audit_events",
                columns: new[] { "entity_type", "entity_id", "timestamp" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_request_id",
                table: "audit_events",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_timestamp_id",
                table: "audit_events",
                columns: new[] { "timestamp", "id" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_career_directions_author_id",
                table: "career_directions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_level_positions_position_id",
                table: "career_level_positions",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_level_positions_track_id_position_id",
                table: "career_level_positions",
                columns: new[] { "track_id", "position_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_author_id",
                table: "career_levels",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ux_career_levels_track_position",
                table: "career_levels",
                columns: new[] { "track_id", "position" },
                unique: true);

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
                name: "IX_competencies_direction_id",
                table: "competencies",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_scale_id",
                table: "competencies",
                column: "scale_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_author_id",
                table: "competency_blocks",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_scope_unit_id",
                table: "competency_blocks",
                column: "scope_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_direction_id",
                table: "competency_matrices",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_org_unit_id_position_id",
                table: "competency_matrices",
                columns: new[] { "org_unit_id", "position_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_org_unit_id_track_id",
                table: "competency_matrices",
                columns: new[] { "org_unit_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_position_id",
                table: "competency_matrices",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_track_id",
                table: "competency_matrices",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_scales_author_id",
                table: "competency_scales",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ux_competency_scales_default",
                table: "competency_scales",
                column: "is_default",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_competency_versions_changed_by_id",
                table: "competency_versions",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_full_name_trgm",
                table: "employees",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_org_unit_id",
                table: "employees",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_position_id",
                table: "employees",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_search_vector",
                table: "employees",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_version_decisions_actor_id",
                table: "matrix_version_decisions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_version_variants_created_by_id",
                table: "matrix_version_variants",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_author_id",
                table: "matrix_versions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_matrix_id_number",
                table: "matrix_versions",
                columns: new[] { "matrix_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_matrix_versions_in_work",
                table: "matrix_versions",
                column: "matrix_id",
                unique: true,
                filter: "status IN ('Draft', 'OnReview', 'Returned')");

            migrationBuilder.CreateIndex(
                name: "ux_matrix_versions_published",
                table: "matrix_versions",
                column: "matrix_id",
                unique: true,
                filter: "status = 'Published'");

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_hr_employee_id",
                table: "org_unit_hr",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_positions_position_id",
                table: "org_unit_positions",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_org_units_head_employee_id",
                table: "org_units",
                column: "head_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_org_units_name",
                table: "org_units",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_org_units_parent_id",
                table: "org_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_org_units_search_vector",
                table: "org_units",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_positions_search_vector",
                table: "positions",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                table: "users",
                column: "employee_id",
                unique: true,
                filter: "employee_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_link_token_hash",
                table: "users",
                column: "link_token_hash",
                unique: true,
                filter: "link_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_user_name",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_career_directions_users_author_id",
                table: "career_directions",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_level_positions_career_levels_level_id",
                table: "career_level_positions",
                column: "level_id",
                principalTable: "career_levels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_level_positions_career_tracks_track_id",
                table: "career_level_positions",
                column: "track_id",
                principalTable: "career_tracks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_levels_career_tracks_track_id",
                table: "career_levels",
                column: "track_id",
                principalTable: "career_tracks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_levels_users_author_id",
                table: "career_levels",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_tracks_org_units_owner_org_unit_id",
                table: "career_tracks",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_tracks_users_author_id",
                table: "career_tracks",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competencies_competency_scales_scale_id",
                table: "competencies",
                column: "scale_id",
                principalTable: "competency_scales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competencies_users_author_id",
                table: "competencies",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_blocks_org_units_scope_unit_id",
                table: "competency_blocks",
                column: "scope_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_blocks_users_author_id",
                table: "competency_blocks",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_matrices_org_units_org_unit_id",
                table: "competency_matrices",
                column: "org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_scales_users_author_id",
                table: "competency_scales",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_versions_users_changed_by_id",
                table: "competency_versions",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_org_units_org_unit_id",
                table: "employees",
                column: "org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE FUNCTION audit_events_reject_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_events is append-only: % is rejected', TG_OP
                        USING ERRCODE = 'restrict_violation';
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER audit_events_reject_update_delete
                    BEFORE UPDATE OR DELETE ON audit_events
                    FOR EACH ROW EXECUTE FUNCTION audit_events_reject_change();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER audit_events_reject_truncate
                    BEFORE TRUNCATE ON audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION audit_events_reject_change();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE audit_events
                    ENABLE ALWAYS TRIGGER audit_events_reject_update_delete,
                    ENABLE ALWAYS TRIGGER audit_events_reject_truncate;
                """);

            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_positions_name_lower" ON positions (lower(name));""");

            migrationBuilder.Sql("""
                INSERT INTO competency_scales (id, points, version, created_at, name, status, is_default, author_id) VALUES
                    ('3004a734-475a-4e4c-b6a6-18275f5a1d5c',
                     '[{"score":0,"label":"Компетенция не подтверждена"},{"score":1,"label":"Выполняет работу преимущественно с поддержкой"},{"score":2,"label":"Самостоятельно решает типовые задачи"},{"score":3,"label":"Самостоятельно решает сложные и неопределённые задачи, способен помогать другим"},{"score":4,"label":"Формирует подходы и стандарты, решает наиболее сложные задачи и развивает других"}]',
                     0, now(), 'Базовая шкала', 'Active', true, NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_positions_position_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_org_units_org_unit_id",
                table: "employees");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "career_level_positions");

            migrationBuilder.DropTable(
                name: "competency_blocks");

            migrationBuilder.DropTable(
                name: "competency_versions");

            migrationBuilder.DropTable(
                name: "matrix_version_decisions");

            migrationBuilder.DropTable(
                name: "matrix_version_variants");

            migrationBuilder.DropTable(
                name: "org_unit_hr");

            migrationBuilder.DropTable(
                name: "org_unit_positions");

            migrationBuilder.DropTable(
                name: "career_levels");

            migrationBuilder.DropTable(
                name: "competencies");

            migrationBuilder.DropTable(
                name: "matrix_versions");

            migrationBuilder.DropTable(
                name: "competency_scales");

            migrationBuilder.DropTable(
                name: "competency_matrices");

            migrationBuilder.DropTable(
                name: "career_tracks");

            migrationBuilder.DropTable(
                name: "career_directions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "org_units");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.Sql("DROP FUNCTION audit_events_reject_change();");
        }
    }
}
