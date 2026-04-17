using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VotingStatus",
                table: "GameNightEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameNightEventId = table.Column<int>(type: "int", nullable: false),
                    GameNightEventGameId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameVotes_GameNightEventGames_GameNightEventGameId",
                        column: x => x.GameNightEventGameId,
                        principalTable: "GameNightEventGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameVotes_GameNightEvents_GameNightEventId",
                        column: x => x.GameNightEventId,
                        principalTable: "GameNightEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameVotes_GameNightEventGameId",
                table: "GameVotes",
                column: "GameNightEventGameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameVotes_GameNightEventId_GameNightEventGameId_UserId",
                table: "GameVotes",
                columns: new[] { "GameNightEventId", "GameNightEventGameId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVotes_UserId",
                table: "GameVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVotes");

            migrationBuilder.DropColumn(
                name: "VotingStatus",
                table: "GameNightEvents");
        }
    }
}
