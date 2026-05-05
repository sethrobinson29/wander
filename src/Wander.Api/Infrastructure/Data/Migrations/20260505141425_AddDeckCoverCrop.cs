using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckCoverCrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CoverCropHeight",
                table: "Decks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoverCropLeft",
                table: "Decks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoverCropTop",
                table: "Decks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoverCropWidth",
                table: "Decks",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverCropHeight",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "CoverCropLeft",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "CoverCropTop",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "CoverCropWidth",
                table: "Decks");
        }
    }
}
