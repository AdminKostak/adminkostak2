using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KampanyaOnaySistemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentOwnerKullaniciId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnayYorumu",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampaignLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: true),
                    Aksiyon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Yorum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HedefEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignLogs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignLogs_Users_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SmtpSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sifre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GonderenAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GonderenEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SslAktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SmtpSettings",
                columns: new[] { "Id", "GonderenAdi", "GonderenEmail", "Host", "KullaniciAdi", "Port", "Sifre", "SslAktif" },
                values: new object[] { 1, "LCM Sistem", "", "", "", 587, "", true });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "AktifMi", "AyarAdi" },
                values: new object[,]
                {
                    { 6, true, "DashboardOtomatikGuncelleme" },
                    { 7, false, "KampanyaOnayaGonderilsinMi" },
                    { 8, false, "MailBildirimleriAktif" },
                    { 9, false, "OnayMailiGonderilsinMi" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CurrentOwnerKullaniciId",
                table: "Campaigns",
                column: "CurrentOwnerKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignLogs_CampaignId",
                table: "CampaignLogs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignLogs_KullaniciId",
                table: "CampaignLogs",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Users_CurrentOwnerKullaniciId",
                table: "Campaigns",
                column: "CurrentOwnerKullaniciId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Users_CurrentOwnerKullaniciId",
                table: "Campaigns");

            migrationBuilder.DropTable(
                name: "CampaignLogs");

            migrationBuilder.DropTable(
                name: "SmtpSettings");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_CurrentOwnerKullaniciId",
                table: "Campaigns");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "CurrentOwnerKullaniciId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "OnayYorumu",
                table: "Campaigns");
        }
    }
}
