using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBggRankFromGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_BggRank",
                table: "Games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Games_BggRank_Positive",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BggRank",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BggRank",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_BggRank",
                table: "Games",
                column: "BggRank");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Games_BggRank_Positive",
                table: "Games",
                sql: "[BggRank] IS NULL OR [BggRank] > 0");
        }
    }
}
