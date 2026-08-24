using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToModifierOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ModifierOptions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ModifierOptions");
        }
    }
}
