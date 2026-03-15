using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoredGamers.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaygroupInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaygroupInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaygroupId = table.Column<int>(type: "int", nullable: false),
                    InvitedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaygroupInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaygroupInvites_Playgroups_PlaygroupId",
                        column: x => x.PlaygroupId,
                        principalTable: "Playgroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaygroupInvites_PlaygroupId_InvitedUserId",
                table: "PlaygroupInvites",
                columns: new[] { "PlaygroupId", "InvitedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaygroupInvites");
        }
    }
}
