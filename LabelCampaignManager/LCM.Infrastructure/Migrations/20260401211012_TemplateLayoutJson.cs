using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TemplateLayoutJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LayoutJson",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LayoutJson",
                table: "Templates");
        }
    }
}
