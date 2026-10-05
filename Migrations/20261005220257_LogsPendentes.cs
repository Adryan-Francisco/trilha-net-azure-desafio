using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trilha_net_azure_desafio.Migrations
{
    /// <inheritdoc />
    public partial class LogsPendentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogsPendentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JSON = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoAcao = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsPendentes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsPendentes");
        }
    }
}
