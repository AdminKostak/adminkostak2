using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KasaKampanyaAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NormalFiyat",
                table: "Campaigns",
                newName: "OriginalPrice");

            migrationBuilder.RenameColumn(
                name: "Kapsam",
                table: "Campaigns",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "KampanyaFiyat",
                table: "Campaigns",
                newName: "DiscountedPrice");

            migrationBuilder.AddColumn<string>(
                name: "CampaignDescription",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailText",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocalProduction",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MinBasketText",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginCountry",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceUpdateDate",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subheadline",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignDescription",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DetailText",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Headline",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsLocalProduction",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MinBasketText",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "OriginCountry",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "PriceUpdateDate",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Subheadline",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "Campaigns",
                newName: "Kapsam");

            migrationBuilder.RenameColumn(
                name: "OriginalPrice",
                table: "Campaigns",
                newName: "NormalFiyat");

            migrationBuilder.RenameColumn(
                name: "DiscountedPrice",
                table: "Campaigns",
                newName: "KampanyaFiyat");
        }
    }
}
