using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EslJobVeGonderimLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EslJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalismaZamani = table.Column<TimeOnly>(type: "time", nullable: false),
                    AktifGonder = table.Column<bool>(type: "bit", nullable: false),
                    PlanlanmisGonder = table.Column<bool>(type: "bit", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    SonCalisma = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslJobs_Users_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EslGonderimLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderimZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tetikleyen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EslJobId = table.Column<int>(type: "int", nullable: true),
                    KullaniciId = table.Column<int>(type: "int", nullable: true),
                    StoreCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToplamKampanya = table.Column<int>(type: "int", nullable: false),
                    BasariliKampanya = table.Column<int>(type: "int", nullable: false),
                    BasarisizKampanya = table.Column<int>(type: "int", nullable: false),
                    HttpStatusKod = table.Column<int>(type: "int", nullable: false),
                    Basarili = table.Column<bool>(type: "bit", nullable: false),
                    HataMesaji = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GonderilenJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslGonderimLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslGonderimLogs_EslJobs_EslJobId",
                        column: x => x.EslJobId,
                        principalTable: "EslJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EslGonderimLogs_Users_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EslGonderimLogs_EslJobId",
                table: "EslGonderimLogs",
                column: "EslJobId");

            migrationBuilder.CreateIndex(
                name: "IX_EslGonderimLogs_KullaniciId",
                table: "EslGonderimLogs",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EslJobs_OlusturanKullaniciId",
                table: "EslJobs",
                column: "OlusturanKullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EslGonderimLogs");

            migrationBuilder.DropTable(
                name: "EslJobs");
        }
    }
}
