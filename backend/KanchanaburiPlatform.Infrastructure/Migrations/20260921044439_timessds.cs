using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class timessds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus_PaymentExpiresAt",
                table: "Orders",
                columns: new[] { "PaymentStatus", "PaymentExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatus_PaymentExpiresAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentExpiresAt",
                table: "Orders");
        }
    }
}
