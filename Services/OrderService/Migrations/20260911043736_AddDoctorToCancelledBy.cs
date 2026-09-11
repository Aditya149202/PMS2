using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorToCancelledBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_CancelledBy",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_CancelledBy",
                table: "Orders",
                sql: "CancelledBy IN ('ADMIN','SYSTEM','DOCTOR')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_CancelledBy",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_CancelledBy",
                table: "Orders",
                sql: "CancelledBy IN ('ADMIN','SYSTEM')");
        }
    }
}
