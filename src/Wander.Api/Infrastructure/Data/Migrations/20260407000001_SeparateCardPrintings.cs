using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateCardPrintings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardPrintings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScryfallId = table.Column<string>(type: "text", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetCode = table.Column<string>(type: "text", nullable: false),
                    CollectorNumber = table.Column<string>(type: "text", nullable: false),
                    ImageUriNormal = table.Column<string>(type: "text", nullable: true),
                    ImageUriSmall = table.Column<string>(type: "text", nullable: true),
                    ImageUriArtCrop = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPrintings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardPrintings_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Migrate existing image/set data into CardPrintings (one printing per oracle card)
            migrationBuilder.Sql(@"
                INSERT INTO ""CardPrintings"" (""Id"", ""ScryfallId"", ""CardId"", ""SetCode"", ""CollectorNumber"", ""ImageUriNormal"", ""ImageUriSmall"", ""ImageUriArtCrop"", ""UpdatedAt"")
                SELECT gen_random_uuid(), ""ScryfallId"", ""Id"", ""SetCode"", ""CollectorNumber"", ""ImageUriNormal"", ""ImageUriSmall"", ""ImageUriArtCrop"", ""UpdatedAt""
                FROM ""Cards"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CardPrintings_CardId",
                table: "CardPrintings",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPrintings_ScryfallId",
                table: "CardPrintings",
                column: "ScryfallId",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrintingId",
                table: "DeckCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeckCards_PrintingId",
                table: "DeckCards",
                column: "PrintingId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeckCards_CardPrintings_PrintingId",
                table: "DeckCards",
                column: "PrintingId",
                principalTable: "CardPrintings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(name: "SetCode", table: "Cards");
            migrationBuilder.DropColumn(name: "CollectorNumber", table: "Cards");
            migrationBuilder.DropColumn(name: "ImageUriNormal", table: "Cards");
            migrationBuilder.DropColumn(name: "ImageUriSmall", table: "Cards");
            migrationBuilder.DropColumn(name: "ImageUriArtCrop", table: "Cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeckCards_CardPrintings_PrintingId",
                table: "DeckCards");

            migrationBuilder.DropIndex(
                name: "IX_DeckCards_PrintingId",
                table: "DeckCards");

            migrationBuilder.DropColumn(name: "PrintingId", table: "DeckCards");

            migrationBuilder.DropTable(name: "CardPrintings");

            migrationBuilder.AddColumn<string>(
                name: "SetCode",
                table: "Cards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CollectorNumber",
                table: "Cards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUriNormal",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUriSmall",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUriArtCrop",
                table: "Cards",
                type: "text",
                nullable: true);
        }
    }
}
