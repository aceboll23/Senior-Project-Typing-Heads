using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAvailableForTrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForTrade",
                table: "UserGameCollections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailableForTrade",
                table: "UserGameCollections");
        }
    }
}
