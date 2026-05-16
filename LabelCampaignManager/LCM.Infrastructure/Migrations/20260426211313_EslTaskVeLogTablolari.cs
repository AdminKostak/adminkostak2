using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EslTaskVeLogTablolari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EslTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SqlScript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LedColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LedCount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LedOnTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LedOffTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LedSleepTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslTasks_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EslTaskLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EslTaskId = table.Column<int>(type: "int", nullable: false),
                    LogTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasariliMi = table.Column<bool>(type: "bit", nullable: false),
                    HataMesaji = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GonderilenJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EslSayisi = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslTaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslTaskLogs_EslTasks_EslTaskId",
                        column: x => x.EslTaskId,
                        principalTable: "EslTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EslTaskLogs_EslTaskId",
                table: "EslTaskLogs",
                column: "EslTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_EslTasks_StoreId",
                table: "EslTasks",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EslTaskLogs");

            migrationBuilder.DropTable(
                name: "EslTasks");
        }
    }
}
