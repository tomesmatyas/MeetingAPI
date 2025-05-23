using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingAPI.Migrations
{
    /// <inheritdoc />
    public partial class V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Interval",
                table: "MeetingRecurrences");

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Meetings",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => new { x.AdminId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AdminUsers_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_UserId",
                table: "AdminUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Meetings");

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "MeetingRecurrences",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
