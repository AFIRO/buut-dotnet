using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEnumFromIntToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Role",
                type: "nvarchar(50)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Title_EN = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Message_EN = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Title_NL = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Message_NL = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.AlterColumn<int>(
                name: "Name",
                table: "Role",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

            migrationBuilder.AddColumn<int>(
                name: "TimeSlot",
                table: "Booking",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
