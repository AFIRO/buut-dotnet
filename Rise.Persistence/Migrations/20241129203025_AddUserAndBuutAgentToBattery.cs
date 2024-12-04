using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndBuutAgentToBattery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Booking_BatteryId",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "IX_Booking_BoatId",
                table: "Booking");

            migrationBuilder.AddColumn<string>(
                name: "BatteryBuutAgentId",
                table: "Battery",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentUserId",
                table: "Battery",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Booking_BatteryId",
                table: "Booking",
                column: "BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_BoatId",
                table: "Booking",
                column: "BoatId");

            migrationBuilder.CreateIndex(
                name: "IX_Battery_BatteryBuutAgentId",
                table: "Battery",
                column: "BatteryBuutAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Battery_CurrentUserId",
                table: "Battery",
                column: "CurrentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Battery_User_BatteryBuutAgentId",
                table: "Battery",
                column: "BatteryBuutAgentId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Battery_User_CurrentUserId",
                table: "Battery",
                column: "CurrentUserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Battery_User_BatteryBuutAgentId",
                table: "Battery");

            migrationBuilder.DropForeignKey(
                name: "FK_Battery_User_CurrentUserId",
                table: "Battery");

            migrationBuilder.DropIndex(
                name: "IX_Booking_BatteryId",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "IX_Booking_BoatId",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "IX_Battery_BatteryBuutAgentId",
                table: "Battery");

            migrationBuilder.DropIndex(
                name: "IX_Battery_CurrentUserId",
                table: "Battery");

            migrationBuilder.DropColumn(
                name: "BatteryBuutAgentId",
                table: "Battery");

            migrationBuilder.DropColumn(
                name: "CurrentUserId",
                table: "Battery");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_BatteryId",
                table: "Booking",
                column: "BatteryId",
                unique: true,
                filter: "[BatteryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_BoatId",
                table: "Booking",
                column: "BoatId",
                unique: true,
                filter: "[BoatId] IS NOT NULL");
        }
    }
}
