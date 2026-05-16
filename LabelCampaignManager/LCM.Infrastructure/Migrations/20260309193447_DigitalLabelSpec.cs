using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DigitalLabelSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigitalLabelSpecs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtiketAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Inch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Olculer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DPI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TahminiPilOmru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesteklenenRenkler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LedDesteklenenRenkler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayanabildigiSicaklik = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveDisplayArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageSwitch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewingAngle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EtiketTipId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalLabelSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalLabelSpecs_LabelTypes_EtiketTipId",
                        column: x => x.EtiketTipId,
                        principalTable: "LabelTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalLabelSpecs_EtiketTipId",
                table: "DigitalLabelSpecs",
                column: "EtiketTipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitalLabelSpecs");
        }
    }
}
