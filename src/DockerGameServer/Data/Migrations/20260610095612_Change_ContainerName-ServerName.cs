using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DockerGameServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class Change_ContainerNameServerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContainerName",
                table: "GameServers",
                newName: "ServerName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ServerName",
                table: "GameServers",
                newName: "ContainerName");
        }
    }
}
