#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class UpdateDeleteBehaviors : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_Currencies_CurrencyId",
            table: "Accounts");

        migrationBuilder.DropForeignKey(
            name: "FK_CustomerOperations_Customers_CustomerId",
            table: "CustomerOperations");

        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Currencies_CurrencyId",
            table: "Payments");

        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Customers_CustomerId",
            table: "Payments");

        migrationBuilder.DropForeignKey(
            name: "FK_SaleItems_Products_ProductId",
            table: "SaleItems");

        migrationBuilder.DropForeignKey(
            name: "FK_Sales_Currencies_CurrencyId",
            table: "Sales");

        migrationBuilder.DropForeignKey(
            name: "FK_Sales_Customers_CustomerId",
            table: "Sales");

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_Currencies_CurrencyId",
            table: "Accounts",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_CustomerOperations_Customers_CustomerId",
            table: "CustomerOperations",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Currencies_CurrencyId",
            table: "Payments",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Customers_CustomerId",
            table: "Payments",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_SaleItems_Products_ProductId",
            table: "SaleItems",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Sales_Currencies_CurrencyId",
            table: "Sales",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Sales_Customers_CustomerId",
            table: "Sales",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_Currencies_CurrencyId",
            table: "Accounts");

        migrationBuilder.DropForeignKey(
            name: "FK_CustomerOperations_Customers_CustomerId",
            table: "CustomerOperations");

        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Currencies_CurrencyId",
            table: "Payments");

        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Customers_CustomerId",
            table: "Payments");

        migrationBuilder.DropForeignKey(
            name: "FK_SaleItems_Products_ProductId",
            table: "SaleItems");

        migrationBuilder.DropForeignKey(
            name: "FK_Sales_Currencies_CurrencyId",
            table: "Sales");

        migrationBuilder.DropForeignKey(
            name: "FK_Sales_Customers_CustomerId",
            table: "Sales");

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_Currencies_CurrencyId",
            table: "Accounts",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_CustomerOperations_Customers_CustomerId",
            table: "CustomerOperations",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Currencies_CurrencyId",
            table: "Payments",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Customers_CustomerId",
            table: "Payments",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_SaleItems_Products_ProductId",
            table: "SaleItems",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Sales_Currencies_CurrencyId",
            table: "Sales",
            column: "CurrencyId",
            principalTable: "Currencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Sales_Customers_CustomerId",
            table: "Sales",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id");
    }
}
