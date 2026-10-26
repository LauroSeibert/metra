using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "code",
                table: "product",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Produtos existentes: o código é o id (já cresce na ordem de criação); a sequência segue dele.
            migrationBuilder.Sql("UPDATE product SET code = id");
            migrationBuilder.Sql(
                "INSERT INTO document_sequence (name, last_number, created_at, updated_at, version) " +
                "SELECT 'product', COALESCE(MAX(id), 0), UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1 FROM product " +
                "ON DUPLICATE KEY UPDATE last_number = VALUES(last_number)");

            migrationBuilder.CreateIndex(
                name: "ux_product_code",
                table: "product",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_code",
                table: "product");

            migrationBuilder.DropColumn(
                name: "code",
                table: "product");
        }
    }
}
