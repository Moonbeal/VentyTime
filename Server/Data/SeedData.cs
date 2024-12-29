using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Data
{
    public static class SeedData
    {
        public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Get all enum values from UserRole
            var roles = Enum.GetNames(typeof(UserRole));

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedEventsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (!await context.Events.AnyAsync())
            {
                // Create test users if they don't exist
                var testUser1 = await userManager.FindByEmailAsync("test@example.com");
                if (testUser1 == null)
                {
                    testUser1 = new()
                    {
                        UserName = "test@example.com",
                        Email = "test@example.com",
                        EmailConfirmed = true,
                        FirstName = "Test",
                        LastName = "User",
                        Location = "Kyiv, Ukraine",
                        Bio = "Event organizer and tech enthusiast"
                    };
                    await userManager.CreateAsync(testUser1, "Test123!");
                    await userManager.AddToRoleAsync(testUser1, UserRole.User.ToString());
                }

                var testUser2 = await userManager.FindByEmailAsync("organizer@example.com");
                if (testUser2 == null)
                {
                    testUser2 = new()
                    {
                        UserName = "organizer@example.com",
                        Email = "organizer@example.com",
                        EmailConfirmed = true,
                        FirstName = "Event",
                        LastName = "Organizer",
                        Location = "Lviv, Ukraine",
                        Bio = "Professional event manager with 5+ years of experience"
                    };
                    await userManager.CreateAsync(testUser2, "Test123!");
                    await userManager.AddToRoleAsync(testUser2, UserRole.Organizer.ToString());
                }

                var events = new List<Event>
                {
                    new()
                    {
                        Title = "Tech Conference 2024",
                        Description = "Join us for the biggest tech conference in Ukraine! Topics include AI, Web Development, and Cloud Computing.",
                        StartDate = new DateTime(2024, 12, 28, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 30, 18, 0, 0),
                        Location = "UNIT.City, Kyiv",
                        MaxAttendees = 500,
                        Price = 100.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Startup Networking Night",
                        Description = "Connect with fellow entrepreneurs and investors in a casual atmosphere.",
                        StartDate = new DateTime(2024, 12, 26, 18, 0, 0),
                        EndDate = new DateTime(2024, 12, 26, 22, 0, 0),
                        Location = "Platforma Art-Zavod, Kyiv",
                        MaxAttendees = 200,
                        Price = 0.00m,
                        ImageUrl = "/images/events/business-event.jpg",
                        Type = EventType.Social,
                        Category = "Business",
                        Status = EventStatus.Active,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "AI Workshop: Machine Learning Basics",
                        Description = "Learn the fundamentals of machine learning in this hands-on workshop.",
                        StartDate = new DateTime(2024, 12, 27, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 27, 16, 0, 0),
                        Location = "UNIT.City, Kyiv",
                        MaxAttendees = 50,
                        Price = 50.00m,
                        ImageUrl = "/images/events/ai-workshop.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "JavaScript Meetup",
                        Description = "Monthly meetup for JavaScript developers to share knowledge and experiences.",
                        StartDate = new DateTime(2024, 12, 29, 19, 0, 0),
                        EndDate = new DateTime(2024, 12, 29, 21, 0, 0),
                        Location = "Coworking Space Hub, Kyiv",
                        MaxAttendees = 50,
                        Price = 0.00m,
                        ImageUrl = "/images/events/meetup.jpg",
                        Type = EventType.Meetup,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Cloud Computing Webinar",
                        Description = "Learn about the latest trends in cloud computing and DevOps practices.",
                        StartDate = new DateTime(2024, 12, 30, 14, 0, 0),
                        EndDate = new DateTime(2024, 12, 30, 16, 0, 0),
                        Location = "Online",
                        MaxAttendees = 1000,
                        Price = 25.00m,
                        ImageUrl = "/images/events/webinar.jpg",
                        Type = EventType.Webinar,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "GameDev Meetup",
                        Description = "Monthly meetup for game developers to share experiences and showcase projects.",
                        StartDate = new DateTime(2024, 12, 26, 15, 0, 0),
                        EndDate = new DateTime(2024, 12, 26, 18, 0, 0),
                        Location = "iHub, Kyiv",
                        MaxAttendees = 100,
                        Price = 0.00m,
                        ImageUrl = "/images/events/technology-event.jpg",
                        Type = EventType.Meetup,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Cybersecurity Forum 2024",
                        Description = "Expert talks on the latest cybersecurity trends and threats.",
                        StartDate = new DateTime(2024, 12, 31, 9, 0, 0),
                        EndDate = new DateTime(2025, 1, 1, 18, 0, 0),
                        Location = "Lviv IT Park",
                        MaxAttendees = 300,
                        Price = 150.00m,
                        ImageUrl = "images/events/cybersecurity.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        Status = EventStatus.Active,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Jazz Night at Atlas",
                        Description = "An evening of smooth jazz with local and international artists.",
                        StartDate = new DateTime(2024, 12, 25, 18, 0, 0),
                        EndDate = new DateTime(2024, 12, 25, 22, 0, 0),
                        Location = "Atlas Club, Kyiv",
                        MaxAttendees = 200,
                        Price = 75.00m,
                        ImageUrl = "/images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        Status = EventStatus.Active,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Ukrainian Premier League Match",
                        Description = "Dynamo Kyiv vs Shakhtar Donetsk - The biggest match of the season!",
                        StartDate = new DateTime(2024, 12, 27, 14, 0, 0),
                        EndDate = new DateTime(2024, 12, 27, 16, 0, 0),
                        Location = "NSC Olimpiyskiy, Kyiv",
                        MaxAttendees = 70000,
                        Price = 300.00m,
                        ImageUrl = "/images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        Status = EventStatus.Active,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Wine Tasting Evening",
                        Description = "Discover the finest Ukrainian wines paired with local cuisine.",
                        StartDate = new DateTime(2024, 12, 26, 18, 0, 0),
                        EndDate = new DateTime(2024, 12, 26, 21, 0, 0),
                        Location = "Good Wine, Kyiv",
                        MaxAttendees = 40,
                        Price = 120.00m,
                        ImageUrl = "/images/events/food-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        Status = EventStatus.Active,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Contemporary Art Exhibition",
                        Description = "Featuring works from emerging Ukrainian artists exploring modern themes.",
                        StartDate = new DateTime(2024, 12, 25, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 8, 18, 0, 0),
                        Location = "Mystetskyi Arsenal, Kyiv",
                        MaxAttendees = 1000,
                        Price = 50.00m,
                        ImageUrl = "/images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        Status = EventStatus.Active,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    }
                };

                await context.Events.AddRangeAsync(events);
                await context.SaveChangesAsync();
            }
        }
    }
}
