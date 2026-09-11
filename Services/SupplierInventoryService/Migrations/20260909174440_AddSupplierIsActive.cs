using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplierInventoryService.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Suppliers");
        }
    }
}
