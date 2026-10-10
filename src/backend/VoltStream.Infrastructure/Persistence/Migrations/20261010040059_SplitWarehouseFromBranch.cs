using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitWarehouseFromBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Warehouses");

            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                table: "Supplies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                table: "Sales",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                table: "Branches",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1L,
                column: "WarehouseId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Supplies_WarehouseId",
                table: "Supplies",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_WarehouseId",
                table: "Sales",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_WarehouseId",
                table: "Branches",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Warehouses_WarehouseId",
                table: "Sales",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplies_Warehouses_WarehouseId",
                table: "Supplies",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Warehouses_WarehouseId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Warehouses_WarehouseId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Supplies_Warehouses_WarehouseId",
                table: "Supplies");

            migrationBuilder.DropIndex(
                name: "IX_Supplies_WarehouseId",
                table: "Supplies");

            migrationBuilder.DropIndex(
                name: "IX_Sales_WarehouseId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Branches_WarehouseId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Supplies");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Branches");

            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                table: "Warehouses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
