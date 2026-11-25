using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewOrderReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_OrderId_ProductId",
                table: "Reviews",
                columns: new[] { "UserId", "OrderId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_OrderId_ProductId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Reviews");
        }
    }
}
