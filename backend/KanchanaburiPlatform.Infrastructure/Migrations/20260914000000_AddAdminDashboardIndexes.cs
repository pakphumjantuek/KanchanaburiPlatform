using Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations;

[DbContext(typeof(StoreContext))]
[Migration("20260914000000_AddAdminDashboardIndexes")]
public partial class AddAdminDashboardIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Orders_UserId_CreatedAt",
            table: "Orders",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_ShopId_CreatedAt",
            table: "Orders",
            columns: new[] { "ShopId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_PaymentStatus_CreatedAt",
            table: "Orders",
            columns: new[] { "PaymentStatus", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderStatus_CreatedAt",
            table: "Orders",
            columns: new[] { "OrderStatus", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_UserId_CreatedAt", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_ShopId_CreatedAt", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_PaymentStatus_CreatedAt", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_OrderStatus_CreatedAt", table: "Orders");
    }
}
