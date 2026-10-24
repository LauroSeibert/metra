using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "stock_movement",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12);

            migrationBuilder.CreateTable(
                name: "document_sequence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    last_number = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_sequence", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sale",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    number = table.Column<int>(type: "int", nullable: false),
                    partner_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    payment_method = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    installments = table.Column<int>(type: "int", nullable: false),
                    discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    issued_at = table.Column<DateTime>(type: "date", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cancellation_reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partner",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sale_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    sale_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_item_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_item_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_document_sequence_name",
                table: "document_sequence",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_issued_at",
                table: "sale",
                column: "issued_at");

            migrationBuilder.CreateIndex(
                name: "ix_sale_partner_id_issued_at",
                table: "sale",
                columns: new[] { "partner_id", "issued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_status_issued_at",
                table: "sale",
                columns: new[] { "status", "issued_at" });

            migrationBuilder.CreateIndex(
                name: "ux_sale_number",
                table: "sale",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_item_product_id",
                table: "sale_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_item_sale_id",
                table: "sale_item",
                column: "sale_id");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_entry_sale_id",
                table: "financial_entry",
                column: "sale_id",
                principalTable: "sale",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Dados: a sequência das vendas começa em zero (a primeira venda é a de número 1).
            migrationBuilder.Sql(
                """
                INSERT INTO document_sequence (name, last_number, created_at, updated_at, version)
                VALUES ('sale', 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_entry_sale_id",
                table: "financial_entry");

            migrationBuilder.DropTable(
                name: "document_sequence");

            migrationBuilder.DropTable(
                name: "sale_item");

            migrationBuilder.DropTable(
                name: "sale");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "stock_movement",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);
        }
    }
}
