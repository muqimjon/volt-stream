using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDraftItemProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleDraftItems_Products_ProductId",
                table: "SaleDraftItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDraftItems_Products_ProductId",
                table: "SaleDraftItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleDraftItems_Products_ProductId",
                table: "SaleDraftItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDraftItems_Products_ProductId",
                table: "SaleDraftItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
