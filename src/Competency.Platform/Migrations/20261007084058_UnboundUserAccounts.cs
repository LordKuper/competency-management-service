using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Lets an account of the user role exist without an employee, as it must once an employee can be deleted: the check constraint
    /// <c>ck_users_role_employee</c> now only forbids an administrator to have an employee. Up changes no data.
    /// Down restores the stricter check and fails, changing nothing, while any user-role account has no employee:
    /// bind each to an employee first.
    /// </summary>
    public partial class UnboundUserAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role_employee",
                table: "users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role_employee",
                table: "users",
                sql: "role = 'GlobalAdmin' AND employee_id IS NULL OR role = 'User'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role_employee",
                table: "users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role_employee",
                table: "users",
                sql: "role = 'GlobalAdmin' AND employee_id IS NULL OR role = 'User' AND employee_id IS NOT NULL");
        }
    }
}
