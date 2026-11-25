using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductDealRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductDeal",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductDeal");
        }
    }
}
