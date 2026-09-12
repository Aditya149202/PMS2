using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplierInventoryService.Migrations
{
    /// <inheritdoc />
    public partial class fixedprocessedevents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProcessedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false,
                        defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.OrderId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedEvents");
        }
    }
}
