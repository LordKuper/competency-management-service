using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kalibr.Platform.Migrations
{
    /// <summary>
    /// Moves account roles from the <c>users.role</c> column to the Identity tables <c>roles</c> and <c>user_roles</c>, and the responsible HR
    /// of a unit from an account to an employee. Data moves with them: a global administrator keeps the role; an account assigned as a unit's HR
    /// gets the HR role when it is bound to an employee, and the assignment passes to that employee; an assignment of an account bound to no
    /// employee is dropped, since no employee can hold it.
    /// </summary>
    public partial class IdentityRolesAndHrByEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("0991bde9-ccde-4831-a138-eef704ed876c"), "0991bde9-ccde-4831-a138-eef704ed876c", "GlobalAdmin", "GLOBALADMIN" },
                    { new Guid("2d465e5b-a4c6-46af-bae3-926cca129e5e"), "2d465e5b-a4c6-46af-bae3-926cca129e5e", "Hr", "HR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_roles_normalized_name",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.Sql("""
                INSERT INTO user_roles (user_id, role_id)
                SELECT u.id, r.id FROM users u JOIN roles r ON r.name = 'GlobalAdmin' WHERE u.role = 'GlobalAdmin';
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_roles (user_id, role_id)
                SELECT DISTINCT u.id, r.id FROM org_unit_hr h JOIN users u ON u.id = h.user_id JOIN roles r ON r.name = 'Hr'
                WHERE u.employee_id IS NOT NULL;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id",
                table: "org_unit_hr",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE org_unit_hr h SET employee_id = u.employee_id FROM users u WHERE u.id = h.user_id;");

            migrationBuilder.Sql("DELETE FROM org_unit_hr WHERE employee_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "employee_id",
                table: "org_unit_hr",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_org_unit_hr_users_user_id",
                table: "org_unit_hr");

            migrationBuilder.DropIndex(
                name: "IX_org_unit_hr_user_id",
                table: "org_unit_hr");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "org_unit_hr");

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_hr_employee_id",
                table: "org_unit_hr",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "FK_org_unit_hr_employees_employee_id",
                table: "org_unit_hr",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <summary>
        /// Returns to one role column and HR assigned by account. An assignment passes to the account bound to its employee now;
        /// what the old shape cannot hold is lost: every HR role, and every assignment of an employee bound to no account.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_org_unit_hr_employees_employee_id",
                table: "org_unit_hr");

            migrationBuilder.DropIndex(
                name: "IX_org_unit_hr_employee_id",
                table: "org_unit_hr");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "org_unit_hr",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE org_unit_hr h SET user_id = u.id FROM users u WHERE u.employee_id = h.employee_id;");

            migrationBuilder.Sql("DELETE FROM org_unit_hr WHERE user_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "org_unit_hr",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "org_unit_hr");

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_hr_user_id",
                table: "org_unit_hr",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_org_unit_hr_users_user_id",
                table: "org_unit_hr",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users u SET role = CASE WHEN EXISTS (
                    SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'GlobalAdmin')
                    THEN 'GlobalAdmin' ELSE 'User' END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                table: "users",
                sql: "role IN ('User', 'GlobalAdmin')");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
