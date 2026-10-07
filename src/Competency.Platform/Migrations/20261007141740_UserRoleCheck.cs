using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Makes the database reject a user account role other than the known ones, as the last guard behind the application.
    /// Up fails, changing nothing, while any account holds another value.
    /// </summary>
    public partial class UserRoleCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                table: "users",
                sql: "role IN ('User', 'GlobalAdmin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                table: "users");
        }
    }
}
