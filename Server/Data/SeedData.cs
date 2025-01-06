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
                        LastName = "User"
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
                        LastName = "Organizer"
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
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
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Web Development Bootcamp",
                        Description = "Intensive 3-day bootcamp covering modern web development technologies and practices.",
                        StartDate = new DateTime(2024, 12, 15, 9, 0, 0),
                        EndDate = new DateTime(2024, 12, 17, 18, 0, 0),
                        Location = "UNIT.City, Kyiv",
                        MaxAttendees = 30,
                        Price = 200.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Digital Marketing Summit",
                        Description = "Learn the latest digital marketing strategies from industry experts.",
                        StartDate = new DateTime(2024, 12, 20, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 21, 17, 0, 0),
                        Location = "Hilton Kyiv",
                        MaxAttendees = 150,
                        Price = 150.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Conference,
                        Category = "Business",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Classical Music Evening",
                        Description = "An evening of classical masterpieces performed by the National Symphony Orchestra.",
                        StartDate = new DateTime(2024, 12, 22, 19, 0, 0),
                        EndDate = new DateTime(2024, 12, 22, 22, 0, 0),
                        Location = "National Philharmonic, Kyiv",
                        MaxAttendees = 400,
                        Price = 75.00m,
                        ImageUrl = "images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Food Festival",
                        Description = "Taste the best of Ukrainian cuisine with top local restaurants and chefs.",
                        StartDate = new DateTime(2024, 12, 23, 12, 0, 0),
                        EndDate = new DateTime(2024, 12, 24, 20, 0, 0),
                        Location = "Art-zavod Platforma",
                        MaxAttendees = 1000,
                        Price = 25.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Blockchain Development Workshop",
                        Description = "Hands-on workshop on blockchain development and smart contracts.",
                        StartDate = new DateTime(2024, 12, 26, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 26, 17, 0, 0),
                        Location = "Coworking Space Hub, Kyiv",
                        MaxAttendees = 40,
                        Price = 180.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Photography Exhibition",
                        Description = "Exhibition of contemporary Ukrainian photographers.",
                        StartDate = new DateTime(2024, 12, 27, 11, 0, 0),
                        EndDate = new DateTime(2024, 12, 29, 19, 0, 0),
                        Location = "Mystetskyi Arsenal, Kyiv",
                        MaxAttendees = 200,
                        Price = 50.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Yoga in the Park",
                        Description = "Morning yoga session for all skill levels.",
                        StartDate = new DateTime(2024, 12, 28, 8, 0, 0),
                        EndDate = new DateTime(2024, 12, 28, 9, 30, 0),
                        Location = "Mariinsky Park",
                        MaxAttendees = 50,
                        Price = 0.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "UI/UX Design Masterclass",
                        Description = "Learn modern UI/UX design principles and practices.",
                        StartDate = new DateTime(2024, 12, 29, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 29, 18, 0, 0),
                        Location = "iHub, Kyiv",
                        MaxAttendees = 35,
                        Price = 120.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Startup Pitch Night",
                        Description = "Local startups pitch their ideas to investors and experts.",
                        StartDate = new DateTime(2024, 12, 30, 18, 0, 0),
                        EndDate = new DateTime(2024, 12, 30, 21, 0, 0),
                        Location = "UNIT.City, Kyiv",
                        MaxAttendees = 100,
                        Price = 0.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Business",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Art & Wine Evening",
                        Description = "Enjoy fine wines while creating your own masterpiece with professional artists.",
                        StartDate = new DateTime(2024, 12, 16, 19, 0, 0),
                        EndDate = new DateTime(2024, 12, 16, 22, 0, 0),
                        Location = "Art Studio Hub, Kyiv",
                        MaxAttendees = 25,
                        Price = 85.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Street Dance Battle",
                        Description = "Annual street dance competition featuring the best dancers from Ukraine.",
                        StartDate = new DateTime(2024, 12, 17, 14, 0, 0),
                        EndDate = new DateTime(2024, 12, 17, 20, 0, 0),
                        Location = "Caribbean Club, Kyiv",
                        MaxAttendees = 300,
                        Price = 30.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Coffee Festival",
                        Description = "Discover the best coffee shops and roasters in Ukraine.",
                        StartDate = new DateTime(2024, 12, 18, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 19, 18, 0, 0),
                        Location = "D12, Kyiv",
                        MaxAttendees = 500,
                        Price = 15.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Mobile App Development Workshop",
                        Description = "Learn to build mobile apps using React Native and Flutter.",
                        StartDate = new DateTime(2024, 12, 19, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 19, 17, 0, 0),
                        Location = "Kooperativ, Kyiv",
                        MaxAttendees = 40,
                        Price = 150.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Board Games Night",
                        Description = "Social gathering for board game enthusiasts of all levels.",
                        StartDate = new DateTime(2024, 12, 21, 18, 0, 0),
                        EndDate = new DateTime(2024, 12, 21, 23, 0, 0),
                        Location = "Gameplay, Kyiv",
                        MaxAttendees = 50,
                        Price = 10.00m,
                        ImageUrl = "images/events/social-event.jpg",
                        Type = EventType.Social,
                        Category = "Other",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Cryptocurrency Trading Seminar",
                        Description = "Expert insights into cryptocurrency trading and blockchain technology.",
                        StartDate = new DateTime(2024, 12, 22, 10, 0, 0),
                        EndDate = new DateTime(2024, 12, 22, 16, 0, 0),
                        Location = "IQ Business Center, Kyiv",
                        MaxAttendees = 80,
                        Price = 200.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Conference,
                        Category = "Business",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Stand-up Comedy Night",
                        Description = "Evening of laughs with Ukraine's top comedians.",
                        StartDate = new DateTime(2024, 12, 23, 20, 0, 0),
                        EndDate = new DateTime(2024, 12, 23, 22, 30, 0),
                        Location = "Caribbean Club, Kyiv",
                        MaxAttendees = 200,
                        Price = 45.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Fitness Boot Camp",
                        Description = "Intensive outdoor fitness training session for all fitness levels.",
                        StartDate = new DateTime(2024, 12, 24, 8, 0, 0),
                        EndDate = new DateTime(2024, 12, 24, 10, 0, 0),
                        Location = "Hidropark, Kyiv",
                        MaxAttendees = 30,
                        Price = 20.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Ukrainian Wine Tasting",
                        Description = "Discover the finest wines from Ukrainian vineyards.",
                        StartDate = new DateTime(2024, 12, 25, 19, 0, 0),
                        EndDate = new DateTime(2024, 12, 25, 22, 0, 0),
                        Location = "Good Wine, Kyiv",
                        MaxAttendees = 40,
                        Price = 70.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Social,
                        Category = "Food & Drink",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Winter Photography Workshop",
                        Description = "Master the art of winter photography in urban and nature settings.",
                        StartDate = new DateTime(2025, 1, 5, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 5, 16, 0, 0),
                        Location = "Feofania Park, Kyiv",
                        MaxAttendees = 20,
                        Price = 90.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Ice Skating Party",
                        Description = "Fun evening of ice skating with music and hot drinks.",
                        StartDate = new DateTime(2025, 1, 8, 18, 0, 0),
                        EndDate = new DateTime(2025, 1, 8, 21, 0, 0),
                        Location = "VDNG Ice Arena, Kyiv",
                        MaxAttendees = 100,
                        Price = 25.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Social,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Data Science Conference",
                        Description = "Latest trends in AI, Machine Learning, and Big Data Analytics.",
                        StartDate = new DateTime(2025, 1, 12, 9, 0, 0),
                        EndDate = new DateTime(2025, 1, 13, 18, 0, 0),
                        Location = "Parkovy Convention Center, Kyiv",
                        MaxAttendees = 300,
                        Price = 250.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Ukrainian Craft Beer Festival",
                        Description = "Sample the best craft beers from local breweries.",
                        StartDate = new DateTime(2025, 1, 15, 14, 0, 0),
                        EndDate = new DateTime(2025, 1, 15, 22, 0, 0),
                        Location = "Art-zavod Platforma",
                        MaxAttendees = 500,
                        Price = 35.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Financial Planning Workshop",
                        Description = "Learn effective strategies for personal finance management.",
                        StartDate = new DateTime(2025, 1, 18, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 18, 16, 0, 0),
                        Location = "IQ Business Center, Kyiv",
                        MaxAttendees = 50,
                        Price = 120.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Business",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Theater Workshop",
                        Description = "Acting and improvisation workshop for beginners.",
                        StartDate = new DateTime(2025, 1, 20, 18, 0, 0),
                        EndDate = new DateTime(2025, 1, 20, 21, 0, 0),
                        Location = "ProEnglish Theatre, Kyiv",
                        MaxAttendees = 15,
                        Price = 60.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Winter Marathon",
                        Description = "10km winter run through the scenic parks of Kyiv.",
                        StartDate = new DateTime(2025, 1, 25, 9, 0, 0),
                        EndDate = new DateTime(2025, 1, 25, 13, 0, 0),
                        Location = "Truhaniv Island, Kyiv",
                        MaxAttendees = 200,
                        Price = 40.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Cybersecurity Workshop",
                        Description = "Practical workshop on personal and business cybersecurity.",
                        StartDate = new DateTime(2025, 1, 28, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 28, 17, 0, 0),
                        Location = "UNIT.City, Kyiv",
                        MaxAttendees = 40,
                        Price = 180.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Cooking Masterclass",
                        Description = "Learn to cook traditional Ukrainian dishes with a modern twist.",
                        StartDate = new DateTime(2025, 1, 30, 15, 0, 0),
                        EndDate = new DateTime(2025, 1, 30, 19, 0, 0),
                        Location = "Culinary Hub, Kyiv",
                        MaxAttendees = 20,
                        Price = 95.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Virtual Reality Gaming Day",
                        Description = "Experience the latest in VR gaming technology.",
                        StartDate = new DateTime(2025, 1, 6, 12, 0, 0),
                        EndDate = new DateTime(2025, 1, 6, 20, 0, 0),
                        Location = "VR Zone, Kyiv",
                        MaxAttendees = 60,
                        Price = 45.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Other,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Jazz & Blues Evening",
                        Description = "Live performances by local jazz and blues musicians.",
                        StartDate = new DateTime(2025, 1, 9, 19, 0, 0),
                        EndDate = new DateTime(2025, 1, 9, 23, 0, 0),
                        Location = "Caribbean Club, Kyiv",
                        MaxAttendees = 150,
                        Price = 55.00m,
                        ImageUrl = "images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Startup Legal Workshop",
                        Description = "Legal aspects of starting and running a business in Ukraine.",
                        StartDate = new DateTime(2025, 1, 14, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 14, 16, 0, 0),
                        Location = "Legal Hub, Kyiv",
                        MaxAttendees = 40,
                        Price = 150.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Business",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Winter Night Run",
                        Description = "5km night run through illuminated city streets.",
                        StartDate = new DateTime(2025, 1, 17, 20, 0, 0),
                        EndDate = new DateTime(2025, 1, 17, 22, 0, 0),
                        Location = "Khreshchatyk Street",
                        MaxAttendees = 300,
                        Price = 30.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Modern Art Exhibition",
                        Description = "Exhibition of contemporary Ukrainian artists.",
                        StartDate = new DateTime(2025, 1, 21, 11, 0, 0),
                        EndDate = new DateTime(2025, 1, 23, 19, 0, 0),
                        Location = "Mystetskyi Arsenal",
                        MaxAttendees = 200,
                        Price = 40.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Python Programming Workshop",
                        Description = "Hands-on Python programming for beginners.",
                        StartDate = new DateTime(2025, 1, 24, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 24, 17, 0, 0),
                        Location = "UNIT.City",
                        MaxAttendees = 30,
                        Price = 120.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Wine & Cheese Tasting",
                        Description = "Guided tasting of Ukrainian wines and artisanal cheeses.",
                        StartDate = new DateTime(2025, 1, 26, 18, 0, 0),
                        EndDate = new DateTime(2025, 1, 26, 21, 0, 0),
                        Location = "Good Wine",
                        MaxAttendees = 30,
                        Price = 85.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Social,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Digital Marketing Masterclass",
                        Description = "Advanced digital marketing strategies and tools.",
                        StartDate = new DateTime(2025, 1, 29, 10, 0, 0),
                        EndDate = new DateTime(2025, 1, 29, 18, 0, 0),
                        Location = "Creative Quarter",
                        MaxAttendees = 50,
                        Price = 180.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Business",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Street Food Festival",
                        Description = "Winter edition of Kyiv's favorite street food festival.",
                        StartDate = new DateTime(2025, 1, 31, 12, 0, 0),
                        EndDate = new DateTime(2025, 2, 2, 20, 0, 0),
                        Location = "Art-zavod Platforma",
                        MaxAttendees = 1000,
                        Price = 20.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "AI in Healthcare Conference",
                        Description = "Exploring AI applications in modern healthcare.",
                        StartDate = new DateTime(2025, 2, 3, 9, 0, 0),
                        EndDate = new DateTime(2025, 2, 4, 18, 0, 0),
                        Location = "Olympic Medical Center",
                        MaxAttendees = 200,
                        Price = 280.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Valentine's Cooking Class",
                        Description = "Learn to cook a romantic dinner for Valentine's Day.",
                        StartDate = new DateTime(2025, 2, 7, 18, 0, 0),
                        EndDate = new DateTime(2025, 2, 7, 21, 0, 0),
                        Location = "Culinary Hub",
                        MaxAttendees = 15,
                        Price = 120.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Blockchain Development Conference",
                        Description = "Latest trends in blockchain and cryptocurrency development.",
                        StartDate = new DateTime(2025, 2, 10, 10, 0, 0),
                        EndDate = new DateTime(2025, 2, 11, 17, 0, 0),
                        Location = "UNIT.City",
                        MaxAttendees = 250,
                        Price = 200.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Valentine's Classical Concert",
                        Description = "Romantic classical music performed by the National Symphony.",
                        StartDate = new DateTime(2025, 2, 14, 19, 0, 0),
                        EndDate = new DateTime(2025, 2, 14, 21, 30, 0),
                        Location = "National Philharmonic",
                        MaxAttendees = 400,
                        Price = 100.00m,
                        ImageUrl = "images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Winter Photography Exhibition",
                        Description = "Exhibition of winter photography from around Ukraine.",
                        StartDate = new DateTime(2025, 2, 17, 11, 0, 0),
                        EndDate = new DateTime(2025, 2, 20, 19, 0, 0),
                        Location = "Izone Creative Community",
                        MaxAttendees = 150,
                        Price = 30.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "E-commerce Strategy Workshop",
                        Description = "Building and scaling successful e-commerce businesses.",
                        StartDate = new DateTime(2025, 2, 21, 10, 0, 0),
                        EndDate = new DateTime(2025, 2, 21, 17, 0, 0),
                        Location = "Creative Quarter",
                        MaxAttendees = 40,
                        Price = 160.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Business",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Indoor Rock Climbing Competition",
                        Description = "Amateur climbing competition with categories for all levels.",
                        StartDate = new DateTime(2025, 2, 23, 9, 0, 0),
                        EndDate = new DateTime(2025, 2, 23, 18, 0, 0),
                        Location = "Climbing Club X-Park",
                        MaxAttendees = 100,
                        Price = 50.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "UI/UX Design Conference",
                        Description = "Latest trends and best practices in UI/UX design.",
                        StartDate = new DateTime(2025, 2, 25, 9, 0, 0),
                        EndDate = new DateTime(2025, 2, 26, 18, 0, 0),
                        Location = "Parkovy Convention Center",
                        MaxAttendees = 300,
                        Price = 220.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Craft Beer Workshop",
                        Description = "Learn the art of craft beer brewing from master brewers.",
                        StartDate = new DateTime(2025, 2, 28, 15, 0, 0),
                        EndDate = new DateTime(2025, 2, 28, 19, 0, 0),
                        Location = "Varvar Brew",
                        MaxAttendees = 25,
                        Price = 90.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Yoga and Meditation Retreat",
                        Description = "Full-day yoga and meditation retreat for inner peace.",
                        StartDate = new DateTime(2025, 2, 28, 9, 0, 0),
                        EndDate = new DateTime(2025, 2, 28, 17, 0, 0),
                        Location = "Botanical Garden",
                        MaxAttendees = 30,
                        Price = 75.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    // Події на березень
                    new()
                    {
                        Title = "Women in Tech Conference",
                        Description = "Celebrating women's achievements in technology and innovation.",
                        StartDate = new DateTime(2025, 3, 1, 9, 0, 0),
                        EndDate = new DateTime(2025, 3, 2, 18, 0, 0),
                        Location = "UNIT.City",
                        MaxAttendees = 400,
                        Price = 150.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Fashion Show",
                        Description = "Showcase of Ukrainian designers' spring collections.",
                        StartDate = new DateTime(2025, 3, 3, 19, 0, 0),
                        EndDate = new DateTime(2025, 3, 3, 22, 0, 0),
                        Location = "Mystetskyi Arsenal",
                        MaxAttendees = 300,
                        Price = 80.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Spring Marathon Training",
                        Description = "Professional training session for upcoming spring marathons.",
                        StartDate = new DateTime(2025, 3, 5, 8, 0, 0),
                        EndDate = new DateTime(2025, 3, 5, 11, 0, 0),
                        Location = "Truhaniv Island",
                        MaxAttendees = 50,
                        Price = 35.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "International Women's Day Concert",
                        Description = "Celebration concert featuring female artists.",
                        StartDate = new DateTime(2025, 3, 8, 19, 0, 0),
                        EndDate = new DateTime(2025, 3, 8, 22, 0, 0),
                        Location = "National Opera of Ukraine",
                        MaxAttendees = 800,
                        Price = 120.00m,
                        ImageUrl = "images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Mobile Game Development Workshop",
                        Description = "Hands-on workshop on creating mobile games.",
                        StartDate = new DateTime(2025, 3, 10, 10, 0, 0),
                        EndDate = new DateTime(2025, 3, 10, 18, 0, 0),
                        Location = "Kooperativ",
                        MaxAttendees = 30,
                        Price = 200.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Wine Festival",
                        Description = "Celebration of new spring wine releases from Ukrainian wineries.",
                        StartDate = new DateTime(2025, 3, 12, 14, 0, 0),
                        EndDate = new DateTime(2025, 3, 14, 20, 0, 0),
                        Location = "Art-zavod Platforma",
                        MaxAttendees = 500,
                        Price = 45.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Startup Funding Workshop",
                        Description = "How to secure funding for your startup in 2025.",
                        StartDate = new DateTime(2025, 3, 15, 10, 0, 0),
                        EndDate = new DateTime(2025, 3, 15, 17, 0, 0),
                        Location = "UNIT.City",
                        MaxAttendees = 100,
                        Price = 180.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Business",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Contemporary Dance Performance",
                        Description = "Modern dance performance by Kyiv Dance Company.",
                        StartDate = new DateTime(2025, 3, 17, 19, 0, 0),
                        EndDate = new DateTime(2025, 3, 17, 21, 0, 0),
                        Location = "October Palace",
                        MaxAttendees = 400,
                        Price = 70.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "St. Patrick's Day Party",
                        Description = "Traditional Irish music, dance, and festivities.",
                        StartDate = new DateTime(2025, 3, 17, 18, 0, 0),
                        EndDate = new DateTime(2025, 3, 18, 2, 0, 0),
                        Location = "O'Brien's Irish Pub",
                        MaxAttendees = 200,
                        Price = 25.00m,
                        ImageUrl = "images/events/social-event.jpg",
                        Type = EventType.Social,
                        Category = "Other",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Photography Workshop",
                        Description = "Capture the beauty of spring in urban photography.",
                        StartDate = new DateTime(2025, 3, 19, 15, 0, 0),
                        EndDate = new DateTime(2025, 3, 19, 20, 0, 0),
                        Location = "Botanical Garden",
                        MaxAttendees = 20,
                        Price = 95.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Artificial Intelligence Summit",
                        Description = "Latest developments in AI and machine learning.",
                        StartDate = new DateTime(2025, 3, 21, 9, 0, 0),
                        EndDate = new DateTime(2025, 3, 22, 18, 0, 0),
                        Location = "Parkovy Convention Center",
                        MaxAttendees = 500,
                        Price = 300.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Conference,
                        Category = "Technology",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Equinox Yoga Festival",
                        Description = "Celebrate spring with outdoor yoga and meditation.",
                        StartDate = new DateTime(2025, 3, 23, 8, 0, 0),
                        EndDate = new DateTime(2025, 3, 23, 20, 0, 0),
                        Location = "Mariinskyi Park",
                        MaxAttendees = 150,
                        Price = 50.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Ukrainian Craft Fair",
                        Description = "Exhibition and sale of traditional Ukrainian crafts.",
                        StartDate = new DateTime(2025, 3, 24, 10, 0, 0),
                        EndDate = new DateTime(2025, 3, 26, 18, 0, 0),
                        Location = "Ukrainian House",
                        MaxAttendees = 300,
                        Price = 15.00m,
                        ImageUrl = "images/events/arts-event.jpg",
                        Type = EventType.Other,
                        Category = "Arts & Culture",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Digital Marketing Conference",
                        Description = "Latest trends in digital marketing and social media.",
                        StartDate = new DateTime(2025, 3, 27, 9, 0, 0),
                        EndDate = new DateTime(2025, 3, 28, 18, 0, 0),
                        Location = "IQ Business Center",
                        MaxAttendees = 250,
                        Price = 220.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Conference,
                        Category = "Business",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Spring Chess Tournament",
                        Description = "Open chess tournament for all skill levels.",
                        StartDate = new DateTime(2025, 3, 29, 10, 0, 0),
                        EndDate = new DateTime(2025, 3, 29, 18, 0, 0),
                        Location = "Kyiv Chess Club",
                        MaxAttendees = 64,
                        Price = 40.00m,
                        ImageUrl = "images/events/sports-event.jpg",
                        Type = EventType.Other,
                        Category = "Sports",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Beer Festival",
                        Description = "Featuring new spring releases from local breweries.",
                        StartDate = new DateTime(2025, 3, 29, 12, 0, 0),
                        EndDate = new DateTime(2025, 3, 30, 22, 0, 0),
                        Location = "Art-zavod Platforma",
                        MaxAttendees = 800,
                        Price = 35.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Other,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Web3 Development Workshop",
                        Description = "Practical workshop on blockchain and Web3 development.",
                        StartDate = new DateTime(2025, 3, 30, 10, 0, 0),
                        EndDate = new DateTime(2025, 3, 30, 18, 0, 0),
                        Location = "UNIT.City",
                        MaxAttendees = 40,
                        Price = 180.00m,
                        ImageUrl = "images/events/tech-conference.jpg",
                        Type = EventType.Workshop,
                        Category = "Technology",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    },
                    new()
                    {
                        Title = "Spring Opera Gala",
                        Description = "Gala concert featuring international opera stars.",
                        StartDate = new DateTime(2025, 3, 31, 19, 0, 0),
                        EndDate = new DateTime(2025, 3, 31, 22, 0, 0),
                        Location = "National Opera of Ukraine",
                        MaxAttendees = 800,
                        Price = 150.00m,
                        ImageUrl = "images/events/music-event.jpg",
                        Type = EventType.Other,
                        Category = "Music",
                        IsActive = true,
                        IsFeatured = true,
                        OrganizerId = testUser2.Id,
                        CreatorId = testUser2.Id
                    },
                    new()
                    {
                        Title = "Spring Cooking Workshop",
                        Description = "Seasonal cooking with fresh spring ingredients.",
                        StartDate = new DateTime(2025, 3, 31, 15, 0, 0),
                        EndDate = new DateTime(2025, 3, 31, 19, 0, 0),
                        Location = "Culinary Hub",
                        MaxAttendees = 20,
                        Price = 95.00m,
                        ImageUrl = "images/events/business-event.jpg",
                        Type = EventType.Workshop,
                        Category = "Food & Drink",
                        IsActive = true,
                        OrganizerId = testUser1.Id,
                        CreatorId = testUser1.Id
                    }
                };

                await context.Events.AddRangeAsync(events);
                await context.SaveChangesAsync();
            }
        }
    }
}
