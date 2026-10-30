using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <summary>
    /// Adds secondary assignments, the legal entity directory and the legal entities of the main and the secondary assignments. No data moves:
    /// every existing employee keeps its unit and position as the main assignment. The unique index on <c>lower(name)</c> of legal entities is SQL,
    /// because the model cannot express an index on an expression.
    /// </summary>
    public partial class AssignmentsAndLegalEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_assignments_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_assignments_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "legal_entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_entities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_assignment_legal_entities",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_entity_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_assignment_legal_entities", x => new { x.assignment_id, x.legal_entity_id });
                    table.ForeignKey(
                        name: "FK_employee_assignment_legal_entities_employee_assignments_ass~",
                        column: x => x.assignment_id,
                        principalTable: "employee_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_assignment_legal_entities_legal_entities_legal_ent~",
                        column: x => x.legal_entity_id,
                        principalTable: "legal_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_legal_entities",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_entity_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_legal_entities", x => new { x.employee_id, x.legal_entity_id });
                    table.ForeignKey(
                        name: "FK_employee_legal_entities_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_legal_entities_legal_entities_legal_entity_id",
                        column: x => x.legal_entity_id,
                        principalTable: "legal_entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignment_legal_entities_legal_entity_id",
                table: "employee_assignment_legal_entities",
                column: "legal_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignments_employee_id_org_unit_id_position_id",
                table: "employee_assignments",
                columns: new[] { "employee_id", "org_unit_id", "position_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignments_org_unit_id",
                table: "employee_assignments",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_assignments_position_id",
                table: "employee_assignments",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_legal_entities_legal_entity_id",
                table: "employee_legal_entities",
                column: "legal_entity_id");

            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_legal_entities_name_lower" ON legal_entities (lower(name));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_assignment_legal_entities");

            migrationBuilder.DropTable(
                name: "employee_legal_entities");

            migrationBuilder.DropTable(
                name: "employee_assignments");

            migrationBuilder.DropTable(
                name: "legal_entities");
        }
    }
}
