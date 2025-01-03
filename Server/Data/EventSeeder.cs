using VentyTime.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace VentyTime.Server.Data
{
    public class EventSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventSeeder> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly (string Title, string Category, string Description, string ImageUrl)[] events = {
            // Tech Events
            ("AI & Machine Learning Summit", "Technology", 
             "Join leading AI researchers and practitioners to explore the latest developments in machine learning, deep learning, and artificial intelligence.", "/images/events/ai-workshop.jpg"),
            ("Web Development Bootcamp", "Technology",
             "Intensive 3-day bootcamp covering modern web development frameworks, best practices, and hands-on coding sessions.", "/images/events/workshop.jpg"),
            ("Cybersecurity Conference", "Technology",
             "Learn about the latest cybersecurity threats and defense strategies from industry experts.", "/images/events/conference.jpg"),
            ("Mobile App Development Workshop", "Technology",
             "Hands-on workshop on building cross-platform mobile applications using React Native and Flutter.", "/images/events/technology-event.jpg"),
            ("Cloud Computing Expo", "Technology",
             "Explore cloud technologies, serverless architecture, and microservices with AWS and Azure experts.", "/images/events/industry-expo.jpg"),

            // Business Events
            ("Startup Pitch Night", "Business",
             "Watch innovative startups pitch their ideas to investors and industry leaders.", "/images/events/startup-pitch.jpg"),
            ("Digital Marketing Masterclass", "Business",
             "Learn advanced digital marketing strategies, SEO, and social media optimization.", "/images/events/training.jpg"),
            ("Leadership Summit 2025", "Business",
             "Join CEOs and business leaders for insights on future leadership challenges and opportunities.", "/images/events/innovation-summit.jpg"),
            ("Financial Planning Workshop", "Business",
             "Expert guidance on personal and business financial planning, investment strategies, and risk management.", "/images/events/seminar.jpg"),
            ("Entrepreneurship Forum", "Business",
             "Network with successful entrepreneurs and learn from their experiences in building successful businesses.", "/images/events/networking.jpg"),

            // Arts & Culture
            ("Contemporary Art Exhibition", "Arts & Culture",
             "Featuring works from emerging artists exploring themes of technology and nature.", "/images/events/charity-gala.jpg"),
            ("Jazz Night Under Stars", "Arts & Culture",
             "An evening of jazz music performed by renowned musicians under the open sky.", "/images/events/awards-ceremony.jpg"),
            ("Photography Workshop", "Arts & Culture",
             "Master the art of photography with professional photographers, covering composition, lighting, and editing.", "/images/events/workshop.jpg"),
            ("Theater Performance: Modern Tales", "Arts & Culture",
             "A modern interpretation of classic stories through innovative theatrical performance.", "/images/events/panel-discussion.jpg"),
            ("Cultural Dance Festival", "Arts & Culture",
             "Celebrate diversity through traditional and contemporary dance performances from around the world.", "/images/events/meetup.jpg"),

            // Sports & Fitness
            ("Marathon 2025", "Sports & Fitness",
             "Annual city marathon featuring professional and amateur runners from around the world.", "/images/events/career-fair.jpg"),
            ("Yoga & Meditation Retreat", "Sports & Fitness",
             "Weekend retreat focusing on mindfulness, yoga practices, and meditation techniques.", "/images/events/team-building.jpg"),
            ("CrossFit Championship", "Sports & Fitness",
             "Elite athletes compete in various CrossFit challenges testing strength and endurance.", "/images/events/hackathon.jpg"),
            ("Tennis Tournament", "Sports & Fitness",
             "Professional tennis tournament featuring top players competing for the championship title.", "/images/events/product-launch.jpg"),
            ("Swimming Competition", "Sports & Fitness",
             "Regional swimming competition with events for all age groups and skill levels.", "/images/events/research-symposium.jpg")
        };

        public EventSeeder(
            ApplicationDbContext context,
            ILogger<EventSeeder> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task SeedEvents()
        {
            try
            {
                if (await _context.Events.AnyAsync())
                {
                    _logger.LogInformation("Events already exist in the database");
                    return;
                }

                _logger.LogInformation("Starting to seed events");

                // Create admin user if not exists
                var adminUser = await EnsureAdminUserAsync();
                var organizerUser = await EnsureOrganizerUserAsync();

                var currentDate = new DateTime(2025, 1, 1);
                var random = new Random(123); // Fixed seed for reproducibility

                foreach (var (Title, Category, Description, ImageUrl) in events)
                {
                    // Create 5 instances of each event template with different dates
                    for (int i = 0; i < 5; i++)
                    {
                        var daysToAdd = random.Next(1, 365);
                        var startDate = currentDate.AddDays(daysToAdd);
                        var endDate = startDate.AddHours(random.Next(2, 8));
                        var maxAttendees = random.Next(50, 500);
                        var price = random.Next(0, 20) * 50; // 0, 50, 100, ..., 950

                        var newEvent = new Event
                        {
                            Title = Title + (i > 0 ? $" {i + 1}" : ""),
                            Description = Description,
                            StartDate = startDate,
                            EndDate = endDate,
                            StartTime = new TimeSpan(random.Next(9, 18), 0, 0), // Between 9 AM and 6 PM
                            Location = "Kyiv, Ukraine",
                            MaxAttendees = maxAttendees,
                            Category = Category,
                            Type = (EventType)random.Next(0, 3),
                            Accessibility = (EventAccessibility)random.Next(0, 2),
                            ImageUrl = ImageUrl,
                            Price = price,
                            IsActive = true,
                            IsCancelled = false,
                            CreatorId = adminUser.Id,
                            OrganizerId = organizerUser.Id,
                            CreatedAt = DateTime.UtcNow,
                            IsFeatured = random.Next(0, 5) == 0, // 20% chance to be featured
                            RequiresRegistration = true,
                            AllowWaitlist = true,
                            WaitlistCapacity = maxAttendees / 10
                        };

                        _context.Events.Add(newEvent);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding events");
                throw;
            }
        }

        private async Task<ApplicationUser> EnsureAdminUserAsync()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@ventytime.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@ventytime.com",
                    Email = "admin@ventytime.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(adminUser, "Admin123!");
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
            return adminUser;
        }

        private async Task<ApplicationUser> EnsureOrganizerUserAsync()
        {
            var organizerUser = await _userManager.FindByEmailAsync("organizer@ventytime.com");
            if (organizerUser == null)
            {
                organizerUser = new ApplicationUser
                {
                    UserName = "organizer@ventytime.com",
                    Email = "organizer@ventytime.com",
                    FirstName = "Event",
                    LastName = "Organizer",
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(organizerUser, "Organizer123!");
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create organizer user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                await _userManager.AddToRoleAsync(organizerUser, "Organizer");
            }
            return organizerUser;
        }
    }
}
