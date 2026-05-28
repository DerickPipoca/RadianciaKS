using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptUrlToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptUrl",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptUrl",
                table: "Orders");
        }
    }
}
