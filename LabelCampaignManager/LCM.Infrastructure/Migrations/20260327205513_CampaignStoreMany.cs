using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CampaignStoreMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Stores_StoreId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_StoreId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Campaigns");

            migrationBuilder.CreateTable(
                name: "CampaignStores",
                columns: table => new
                {
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignStores", x => new { x.CampaignId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_CampaignStores_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignStores_StoreId",
                table: "CampaignStores",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignStores");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StoreId",
                table: "Campaigns",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Stores_StoreId",
                table: "Campaigns",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
