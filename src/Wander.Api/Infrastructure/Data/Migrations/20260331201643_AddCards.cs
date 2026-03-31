using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Wander.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScryfallId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ManaCost = table.Column<string>(type: "text", nullable: true),
                    Cmc = table.Column<decimal>(type: "numeric", nullable: false),
                    TypeLine = table.Column<string>(type: "text", nullable: false),
                    OracleText = table.Column<string>(type: "text", nullable: true),
                    Colors = table.Column<List<string>>(type: "text[]", nullable: false),
                    ColorIdentity = table.Column<List<string>>(type: "text[]", nullable: false),
                    ImageUriNormal = table.Column<string>(type: "text", nullable: true),
                    ImageUriSmall = table.Column<string>(type: "text", nullable: true),
                    ImageUriArtCrop = table.Column<string>(type: "text", nullable: true),
                    SetCode = table.Column<string>(type: "text", nullable: false),
                    CollectorNumber = table.Column<string>(type: "text", nullable: false),
                    Legalities = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    NameSearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('english', \"Name\")", stored: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Name",
                table: "Cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_NameSearchVector",
                table: "Cards",
                column: "NameSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ScryfallId",
                table: "Cards",
                column: "ScryfallId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards");
        }
    }
}
