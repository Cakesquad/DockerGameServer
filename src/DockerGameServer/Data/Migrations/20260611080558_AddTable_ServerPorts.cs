using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DockerGameServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_ServerPorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerPorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalPort = table.Column<int>(type: "integer", nullable: false),
                    ExternalPort = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerPorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerPorts_GameServers_GameServerId",
                        column: x => x.GameServerId,
                        principalTable: "GameServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerPorts_ExternalPort",
                table: "ServerPorts",
                column: "ExternalPort",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerPorts_GameServerId",
                table: "ServerPorts",
                column: "GameServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerPorts");
        }
    }
}
