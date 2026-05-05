using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlavorTextToPrintings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlavorText",
                table: "CardPrintings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlavorText",
                table: "CardPrintings");
        }
    }
}
