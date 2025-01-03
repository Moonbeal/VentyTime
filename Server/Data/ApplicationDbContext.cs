using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventComment> EventComments { get; set; } = null!;
        public DbSet<UserEventRegistration> UserEventRegistrations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.AvatarUrl).HasMaxLength(2000);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(200);
            });

            builder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedEvents)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Organizer)
                    .WithMany(u => u.OrganizedEvents)
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Title)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(e => e.Location)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.VenueDetails)
                    .HasMaxLength(200);

                entity.Property(e => e.OnlineUrl)
                    .HasMaxLength(200);

                entity.Property(e => e.OnlineMeetingUrl)
                    .HasMaxLength(200);

                entity.Property(e => e.OnlineMeetingId)
                    .HasMaxLength(50);

                entity.Property(e => e.OnlineMeetingPassword)
                    .HasMaxLength(50);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.EndDate)
                    .IsRequired();

                entity.Property(e => e.StartTime)
                    .IsRequired();

                entity.Property(e => e.MaxAttendees)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.EarlyBirdPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Accessibility)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.RefundPolicy)
                    .HasMaxLength(1000);

                entity.Property(e => e.Requirements)
                    .HasMaxLength(1000);

                entity.Property(e => e.Schedule)
                    .HasMaxLength(2000);

                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => v != null ? string.Join(',', v) : null,
                        v => v != null ? v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : null,
                        new ValueComparer<List<string>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()
                        )
                    );
            });

            builder.Entity<UserEventRegistration>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Registrations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Event)
                    .WithMany(e => e.Registrations)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(r => r.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(r => r.UpdatedAt)
                    .IsRequired(false);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasDefaultValue(RegistrationStatus.Pending);
            });

            builder.Entity<EventComment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.User)
                    .WithMany(u => u.EventComments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Event)
                    .WithMany(e => e.EventComments)
                    .HasForeignKey(c => c.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.Content)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.UpdatedAt)
                    .IsRequired(false);
            });
        }
    }
}
