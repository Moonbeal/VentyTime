using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Identity;

#nullable disable

namespace VentyTime.Server.Data.Migrations
{
    public partial class AddOrganizerRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Organizer role if it doesn't exist
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[] { 
                    "2c5e174e-3b0e-446f-86af-483d56fd7210", 
                    "Organizer", 
                    "ORGANIZER",
                    Guid.NewGuid().ToString() 
                }
            );

            // Add your user to the Organizer role
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { 
                    "b029e422-5974-4463-8de6-c06e86fd0d43", // Your user ID
                    "2c5e174e-3b0e-446f-86af-483d56fd7210"  // Organizer role ID
                }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove your user from the Organizer role
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "UserId", "RoleId" },
                keyValues: new object[] { 
                    "b029e422-5974-4463-8de6-c06e86fd0d43",
                    "2c5e174e-3b0e-446f-86af-483d56fd7210"
                }
            );

            // Remove the Organizer role
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2c5e174e-3b0e-446f-86af-483d56fd7210"
            );
        }
    }
}
