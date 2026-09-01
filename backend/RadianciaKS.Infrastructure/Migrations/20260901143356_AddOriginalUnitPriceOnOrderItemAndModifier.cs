using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalUnitPriceOnOrderItemAndModifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAdditionalPrice",
                table: "OrderItemModifiers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OriginalAdditionalPrice",
                table: "OrderItemModifiers");
        }
    }
}
