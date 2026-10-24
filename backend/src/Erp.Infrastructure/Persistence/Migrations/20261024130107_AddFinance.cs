using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_category",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_category", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "financial_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    kind = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    partner_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    paid_at = table.Column<DateTime>(type: "date", nullable: true),
                    paid_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    installment_no = table.Column<int>(type: "int", nullable: false),
                    installment_total = table.Column<int>(type: "int", nullable: false),
                    sale_id = table.Column<long>(type: "bigint", nullable: true),
                    reversed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reversal_reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_entry_category_id",
                        column: x => x.category_id,
                        principalTable: "financial_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_entry_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partner",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_financial_category_kind_name",
                table: "financial_category",
                columns: new[] { "kind", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_category_id",
                table: "financial_entry",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_kind_paid_at",
                table: "financial_entry",
                columns: new[] { "kind", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_kind_status_due_date_id",
                table: "financial_entry",
                columns: new[] { "kind", "status", "due_date", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_partner_id",
                table: "financial_entry",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_sale_id",
                table: "financial_entry",
                column: "sale_id");

            // Dados: categorias iniciais (o usuário edita, inclui e exclui as que não usar).
            migrationBuilder.Sql(
                """
                INSERT INTO financial_category (name, kind, created_at, updated_at, version) VALUES
                ('Vendas', 'Revenue', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Serviços', 'Revenue', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Outras receitas', 'Revenue', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Fornecedores', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Aluguel', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Salários', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Impostos', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Energia e água', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1),
                ('Outras despesas', 'Expense', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_entry");

            migrationBuilder.DropTable(
                name: "financial_category");
        }
    }
}
