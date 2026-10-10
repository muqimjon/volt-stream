using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VoltStream.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "SaleId",
                table: "PrintJobs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "SaleDraftId",
                table: "PrintJobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaleDrafts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedSaleId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleDrafts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleDrafts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleDrafts_Sales_ConfirmedSaleId",
                        column: x => x.ConfirmedSaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SaleDraftItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollCount = table.Column<int>(type: "integer", nullable: false),
                    LengthPerRoll = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalLength = table.Column<decimal>(type: "numeric", nullable: false),
                    SaleDraftId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleDraftItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleDraftItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleDraftItems_SaleDrafts_SaleDraftId",
                        column: x => x.SaleDraftId,
                        principalTable: "SaleDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_SaleDraftId_Status",
                table: "PrintJobs",
                columns: new[] { "SaleDraftId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrintJobs_SaleOrDraft",
                table: "PrintJobs",
                sql: "(\"SaleId\" IS NULL) <> (\"SaleDraftId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDraftItems_ProductId",
                table: "SaleDraftItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDraftItems_SaleDraftId",
                table: "SaleDraftItems",
                column: "SaleDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDrafts_BranchId_Status",
                table: "SaleDrafts",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDrafts_ConfirmedSaleId",
                table: "SaleDrafts",
                column: "ConfirmedSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDrafts_CustomerId",
                table: "SaleDrafts",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrintJobs_SaleDrafts_SaleDraftId",
                table: "PrintJobs",
                column: "SaleDraftId",
                principalTable: "SaleDrafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrintJobs_SaleDrafts_SaleDraftId",
                table: "PrintJobs");

            migrationBuilder.DropTable(
                name: "SaleDraftItems");

            migrationBuilder.DropTable(
                name: "SaleDrafts");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobs_SaleDraftId_Status",
                table: "PrintJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrintJobs_SaleOrDraft",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "SaleDraftId",
                table: "PrintJobs");

            migrationBuilder.AlterColumn<long>(
                name: "SaleId",
                table: "PrintJobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
