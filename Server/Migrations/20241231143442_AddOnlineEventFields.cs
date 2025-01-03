using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentyTime.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineEventFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OnlineEventUrl",
                table: "Events",
                newName: "OnlineUrl");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "UserEventRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OnlineMeetingId",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineMeetingPassword",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineMeetingUrl",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OnlineMeetingId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OnlineMeetingPassword",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OnlineMeetingUrl",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "OnlineUrl",
                table: "Events",
                newName: "OnlineEventUrl");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "UserEventRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }
    }
}
