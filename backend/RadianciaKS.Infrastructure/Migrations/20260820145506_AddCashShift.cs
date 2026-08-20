using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadianciaKS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashShiftId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashShift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeOpenerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCloserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialBalance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FinalCalculatedBalance = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    FinalReportedBalance = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShift", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashShift_Employees_EmployeeCloserId",
                        column: x => x.EmployeeCloserId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashShift_Employees_EmployeeOpenerId",
                        column: x => x.EmployeeOpenerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CashShiftId",
                table: "Orders",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShift_EmployeeCloserId",
                table: "CashShift",
                column: "EmployeeCloserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShift_EmployeeOpenerId",
                table: "CashShift",
                column: "EmployeeOpenerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShift_TenantId",
                table: "CashShift",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CashShift_CashShiftId",
                table: "Orders",
                column: "CashShiftId",
                principalTable: "CashShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CashShift_CashShiftId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CashShift");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CashShiftId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CashShiftId",
                table: "Orders");
        }
    }
}
