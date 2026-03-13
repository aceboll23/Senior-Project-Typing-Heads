using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateGamesPerEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameNightEventGames_GameNightEventId_GameId_UserId",
                table: "GameNightEventGames");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEventGames_GameNightEventId_GameId",
                table: "GameNightEventGames",
                columns: new[] { "GameNightEventId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameNightEventGames_GameNightEventId_GameId",
                table: "GameNightEventGames");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEventGames_GameNightEventId_GameId_UserId",
                table: "GameNightEventGames",
                columns: new[] { "GameNightEventId", "GameId", "UserId" },
                unique: true);
        }
    }
}
