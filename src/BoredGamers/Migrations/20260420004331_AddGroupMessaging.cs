using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaygroupMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaygroupId = table.Column<int>(type: "int", nullable: false),
                    SenderProfileId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsSystemMessage = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaygroupMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaygroupMessages_Playgroups_PlaygroupId",
                        column: x => x.PlaygroupId,
                        principalTable: "Playgroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaygroupMessages_UserProfile_SenderProfileId",
                        column: x => x.SenderProfileId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaygroupMessages_PlaygroupId_SentAt",
                table: "PlaygroupMessages",
                columns: new[] { "PlaygroupId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaygroupMessages_SenderProfileId",
                table: "PlaygroupMessages",
                column: "SenderProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaygroupMessages");
        }
    }
}
