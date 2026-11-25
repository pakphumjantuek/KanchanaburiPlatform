using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateContentOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Contents",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerShopId",
                table: "Contents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ContentType_OwnerShopId",
                table: "Contents",
                columns: new[] { "ContentType", "OwnerShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_OwnerShopId",
                table: "Contents",
                column: "OwnerShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_Shops_OwnerShopId",
                table: "Contents",
                column: "OwnerShopId",
                principalTable: "Shops",
                principalColumn: "ShopId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contents_Shops_OwnerShopId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Contents_ContentType_OwnerShopId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Contents_OwnerShopId",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "OwnerShopId",
                table: "Contents");
        }
    }
}
