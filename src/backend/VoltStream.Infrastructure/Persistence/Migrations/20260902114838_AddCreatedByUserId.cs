using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "WarehouseStocks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Warehouses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Supplies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Sales",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "SaleItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Products",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Customers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "CustomerOperations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Currencies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Categories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Cashes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "AllowedClients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Accounts",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Supplies");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CustomerOperations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Cashes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AllowedClients");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Accounts");
        }
    }
}
