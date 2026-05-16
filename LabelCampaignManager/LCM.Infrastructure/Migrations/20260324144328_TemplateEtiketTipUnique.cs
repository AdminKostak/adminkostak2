using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TemplateEtiketTipUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Templates_EtiketTipId",
                table: "Templates");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_EtiketTipId",
                table: "Templates",
                column: "EtiketTipId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Templates_EtiketTipId",
                table: "Templates");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_EtiketTipId",
                table: "Templates",
                column: "EtiketTipId");
        }
    }
}
