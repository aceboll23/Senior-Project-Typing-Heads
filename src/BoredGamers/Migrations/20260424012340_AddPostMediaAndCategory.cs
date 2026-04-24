using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddPostMediaAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ProfilePosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "ProfilePosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "ProfilePosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProfilePosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfilePosts_GameId",
                table: "ProfilePosts",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfilePosts_Games_GameId",
                table: "ProfilePosts",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfilePosts_Games_GameId",
                table: "ProfilePosts");

            migrationBuilder.DropIndex(
                name: "IX_ProfilePosts_GameId",
                table: "ProfilePosts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ProfilePosts");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "ProfilePosts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProfilePosts");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ProfilePosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
