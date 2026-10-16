using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietThang.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SearchTextAndCostSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Products",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "OrderDetails",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            // Đơn hàng cũ chưa có snapshot giá vốn: lấy giá vốn hiện tại của biến thể làm giá trị gần đúng nhất
            migrationBuilder.Sql(
                "UPDATE od SET od.UnitCost = v.CostPrice FROM OrderDetails od INNER JOIN ProductVariants v ON v.Id = od.VariantId WHERE od.UnitCost = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "OrderDetails");
        }
    }
}
