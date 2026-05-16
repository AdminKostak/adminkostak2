using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EslFieldMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EslFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SablonId = table.Column<int>(type: "int", nullable: false),
                    BizimAlanAdi = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EslFieldId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMappings_EslFields_EslFieldId",
                        column: x => x.EslFieldId,
                        principalTable: "EslFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldMappings_Templates_SablonId",
                        column: x => x.SablonId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_EslFieldId",
                table: "FieldMappings",
                column: "EslFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_SablonId_BizimAlanAdi",
                table: "FieldMappings",
                columns: new[] { "SablonId", "BizimAlanAdi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "EslFields");
        }
    }
}
