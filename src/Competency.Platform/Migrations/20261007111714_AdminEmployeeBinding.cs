using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Lets an account of the administrator role be bound to an employee: the check constraint <c>ck_users_role_employee</c> is dropped. Up changes no data.
    /// Down restores the check and fails, changing nothing, while any administrator is bound to an employee: unbind each first.
    /// </summary>
    public partial class AdminEmployeeBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role_employee",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role_employee",
                table: "users",
                sql: "role = 'GlobalAdmin' AND employee_id IS NULL OR role = 'User'");
        }
    }
}
