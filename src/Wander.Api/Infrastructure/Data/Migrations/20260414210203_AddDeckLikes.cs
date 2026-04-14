using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeckLike_AspNetUsers_UserId",
                table: "DeckLike");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckLike_Decks_DeckId",
                table: "DeckLike");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeckLike",
                table: "DeckLike");

            migrationBuilder.RenameTable(
                name: "DeckLike",
                newName: "DeckLikes");

            migrationBuilder.RenameIndex(
                name: "IX_DeckLike_DeckId",
                table: "DeckLikes",
                newName: "IX_DeckLikes_DeckId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeckLikes",
                table: "DeckLikes",
                columns: new[] { "UserId", "DeckId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeckLikes_AspNetUsers_UserId",
                table: "DeckLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckLikes_Decks_DeckId",
                table: "DeckLikes",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeckLikes_AspNetUsers_UserId",
                table: "DeckLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckLikes_Decks_DeckId",
                table: "DeckLikes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeckLikes",
                table: "DeckLikes");

            migrationBuilder.RenameTable(
                name: "DeckLikes",
                newName: "DeckLike");

            migrationBuilder.RenameIndex(
                name: "IX_DeckLikes_DeckId",
                table: "DeckLike",
                newName: "IX_DeckLike_DeckId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeckLike",
                table: "DeckLike",
                columns: new[] { "UserId", "DeckId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeckLike_AspNetUsers_UserId",
                table: "DeckLike",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckLike_Decks_DeckId",
                table: "DeckLike",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
