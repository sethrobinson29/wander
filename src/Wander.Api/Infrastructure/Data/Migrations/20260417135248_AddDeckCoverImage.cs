using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverPrintingId",
                table: "Decks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_CoverPrintingId",
                table: "Decks",
                column: "CoverPrintingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_CardPrintings_CoverPrintingId",
                table: "Decks",
                column: "CoverPrintingId",
                principalTable: "CardPrintings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decks_CardPrintings_CoverPrintingId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Decks_CoverPrintingId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "CoverPrintingId",
                table: "Decks");
        }
    }
}
