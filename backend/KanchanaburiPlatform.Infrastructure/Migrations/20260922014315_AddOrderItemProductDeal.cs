using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemProductDeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductDealId",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductDealId",
                table: "OrderItems",
                column: "ProductDealId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductDeal_ProductDealId",
                table: "OrderItems",
                column: "ProductDealId",
                principalTable: "ProductDeal",
                principalColumn: "ProductDealId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductDeal_ProductDealId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductDealId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductDealId",
                table: "OrderItems");
        }
    }
}
