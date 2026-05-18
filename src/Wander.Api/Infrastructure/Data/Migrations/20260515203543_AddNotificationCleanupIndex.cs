using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCleanupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsRead_CreatedAt",
                table: "Notifications");
        }
    }
}
