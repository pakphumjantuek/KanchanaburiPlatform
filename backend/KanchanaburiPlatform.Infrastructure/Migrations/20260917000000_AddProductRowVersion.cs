using Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanchanaburiPlatform.Infrastructure.Migrations;

[DbContext(typeof(StoreContext))]
[Migration("20260917000000_AddProductRowVersion")]
public partial class AddProductRowVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Products",
            type: "rowversion",
            rowVersion: true,
            nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RowVersion", table: "Products");
    }
}
