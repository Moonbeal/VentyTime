using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentyTime.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EventReminders",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewCommentNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewFollowerNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewLikeNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PushNotifications",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EventReminders",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NewCommentNotifications",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NewFollowerNotifications",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NewLikeNotifications",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PushNotifications",
                table: "AspNetUsers");
        }
    }
}
