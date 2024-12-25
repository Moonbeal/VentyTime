using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentyTime.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRegistrationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "Registrations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Registrations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Registrations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Registrations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ApplicationUserId",
                table: "Registrations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_UserId",
                table: "Registrations",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AspNetUsers_ApplicationUserId",
                table: "Registrations",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AspNetUsers_ApplicationUserId",
                table: "Registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ApplicationUserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_UserId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Registrations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Registrations",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Registrations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "Registrations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
