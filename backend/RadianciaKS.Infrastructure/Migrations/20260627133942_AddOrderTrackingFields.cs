using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaidById",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaidById",
                table: "Orders",
                column: "PaidById");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Employees_PaidById",
                table: "Orders",
                column: "PaidById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Employees_PaidById",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaidById",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidById",
                table: "Orders");
        }
    }
}
