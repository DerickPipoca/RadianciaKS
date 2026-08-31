using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionIdToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PromotionId",
                table: "OrderItems",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Promotions_PromotionId",
                table: "OrderItems",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Promotions_PromotionId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_PromotionId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "OrderItems");
        }
    }
}
