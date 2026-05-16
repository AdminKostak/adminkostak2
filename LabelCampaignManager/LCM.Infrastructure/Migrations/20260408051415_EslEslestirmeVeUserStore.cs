using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EslEslestirmeVeUserStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CokluEslestirmeIzni",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HizliEslestirmeIzni",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EslEslestirmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EslBarkod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KampanyaId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    EslestirmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Override = table.Column<bool>(type: "bit", nullable: false),
                    IslemTipi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasariliMi = table.Column<bool>(type: "bit", nullable: false),
                    HataMesaji = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslEslestirmeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslEslestirmeler_Campaigns_KampanyaId",
                        column: x => x.KampanyaId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EslEslestirmeler_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EslEslestirmeler_Users_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserStores",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStores", x => new { x.UserId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_UserStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "AktifMi", "AyarAdi" },
                values: new object[,]
                {
                    { 4, true, "EslOnayPopupAktif" },
                    { 5, true, "EslOverrideAktif" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EslEslestirmeler_KampanyaId",
                table: "EslEslestirmeler",
                column: "KampanyaId");

            migrationBuilder.CreateIndex(
                name: "IX_EslEslestirmeler_KullaniciId",
                table: "EslEslestirmeler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_EslEslestirmeler_StoreId",
                table: "EslEslestirmeler",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStores_StoreId",
                table: "UserStores",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EslEslestirmeler");

            migrationBuilder.DropTable(
                name: "UserStores");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "CokluEslestirmeIzni",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HizliEslestirmeIzni",
                table: "Users");
        }
    }
}
