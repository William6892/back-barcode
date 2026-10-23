using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShippingExitSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBarcodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status_CreatedAt",
                table: "Shipments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScannedItems_Barcode",
                table: "ScannedItems",
                column: "Barcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_Status_CreatedAt",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ScannedItems_Barcode",
                table: "ScannedItems");
        }
    }
}
