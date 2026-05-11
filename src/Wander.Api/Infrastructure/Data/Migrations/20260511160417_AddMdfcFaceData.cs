using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMdfcFaceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackFaceManaCost",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackFaceOracleText",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackFaceTypeLine",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackImageUriNormal",
                table: "CardPrintings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackImageUriSmall",
                table: "CardPrintings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackFaceManaCost",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "BackFaceOracleText",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "BackFaceTypeLine",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "BackImageUriNormal",
                table: "CardPrintings");

            migrationBuilder.DropColumn(
                name: "BackImageUriSmall",
                table: "CardPrintings");
        }
    }
}
