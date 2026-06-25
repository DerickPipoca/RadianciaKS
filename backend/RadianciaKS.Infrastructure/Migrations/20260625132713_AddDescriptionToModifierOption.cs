using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToModifierOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ModifierOptions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ModifierOptions");
        }
    }
}
