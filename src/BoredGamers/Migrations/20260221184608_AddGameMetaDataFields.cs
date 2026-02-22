using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddGameMetaDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinPlayers",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayTime",
                table: "Games",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MinPlayers",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PlayTime",
                table: "Games");
        }
    }
}
