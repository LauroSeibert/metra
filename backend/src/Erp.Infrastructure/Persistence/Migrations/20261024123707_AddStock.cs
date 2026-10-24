using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_setting",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_setting", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_balance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balance", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_balance_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reference_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movement_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_app_setting_key",
                table: "app_setting",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_stock_balance_product_id",
                table: "stock_balance",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_occurred_at_id",
                table: "stock_movement",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_product_id_occurred_at_id",
                table: "stock_movement",
                columns: new[] { "product_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_reference_type_reference_id",
                table: "stock_movement",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_user_id",
                table: "stock_movement",
                column: "user_id");

            // Dados: todo produto já cadastrado ganha saldo zero (os novos nascem com saldo no ProductService).
            migrationBuilder.Sql(
                """
                INSERT INTO stock_balance (product_id, quantity, created_at, updated_at, version)
                SELECT p.id, 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1
                FROM product p
                WHERE NOT EXISTS (SELECT 1 FROM stock_balance b WHERE b.product_id = p.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_setting");

            migrationBuilder.DropTable(
                name: "stock_balance");

            migrationBuilder.DropTable(
                name: "stock_movement");
        }
    }
}
