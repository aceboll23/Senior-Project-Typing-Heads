using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistStatusToUserGameCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserGameCollections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserGameCollections");
        }
    }
}
