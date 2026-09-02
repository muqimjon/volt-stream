using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "SaleItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsWeightTracked",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPerUnit",
                table: "Products",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "IsWeightTracked",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightPerUnit",
                table: "Products");
        }
    }
}
