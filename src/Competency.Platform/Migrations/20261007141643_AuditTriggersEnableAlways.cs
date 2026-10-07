using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Keeps the audit journal append-only for a superuser application account too: ordinary triggers are silenced by
    /// <c>SET session_replication_role = replica</c>, triggers enabled always are not. Up changes no data.
    /// </summary>
    public partial class AuditTriggersEnableAlways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE audit_events
                    ENABLE ALWAYS TRIGGER audit_events_reject_update_delete,
                    ENABLE ALWAYS TRIGGER audit_events_reject_truncate;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE audit_events
                    ENABLE TRIGGER audit_events_reject_update_delete,
                    ENABLE TRIGGER audit_events_reject_truncate;
                """);
        }
    }
}
