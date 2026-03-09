using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddGameNightEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameNightEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaygroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNightEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameNightEvents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameNightEvents_Playgroups_PlaygroupId",
                        column: x => x.PlaygroupId,
                        principalTable: "Playgroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameNightEventGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameNightEventId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNightEventGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameNightEventGames_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameNightEventGames_GameNightEvents_GameNightEventId",
                        column: x => x.GameNightEventId,
                        principalTable: "GameNightEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameNightEventGames_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEventGames_GameId",
                table: "GameNightEventGames",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEventGames_GameNightEventId_GameId_UserId",
                table: "GameNightEventGames",
                columns: new[] { "GameNightEventId", "GameId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEventGames_UserId",
                table: "GameNightEventGames",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEvents_CreatedByUserId",
                table: "GameNightEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEvents_EventDateTime",
                table: "GameNightEvents",
                column: "EventDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_GameNightEvents_PlaygroupId",
                table: "GameNightEvents",
                column: "PlaygroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameNightEventGames");

            migrationBuilder.DropTable(
                name: "GameNightEvents");
        }
    }
}
