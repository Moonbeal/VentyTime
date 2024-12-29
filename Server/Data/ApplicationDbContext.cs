using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;
        public DbSet<Registration> Registrations { get; set; } = null!;
        public DbSet<NotificationMessage> Notifications { get; set; } = null!;

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

                // Ignore OrganizedEvents property
                entity.Ignore(e => e.OrganizedEvents);
            });

            builder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.EndDate)
                    .IsRequired();

                entity.Property(e => e.MaxAttendees)
                    .IsRequired();

                entity.Property(e => e.CreatorId)
                    .IsRequired();

                entity.Property(e => e.OrganizerId)
                    .IsRequired();

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Organizer)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Ignore navigation properties that are not needed
                entity.Ignore(e => e.Creator);
                entity.Ignore(e => e.Organizer);
            });

            builder.Entity<Comment>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Comments)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventRegistration>(entity =>
            {
                entity.HasKey(er => new { er.EventId, er.UserId });

                entity.HasOne<Event>()
                    .WithMany(e => e.EventRegistrations)
                    .HasForeignKey(er => er.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(er => er.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(er => er.RegistrationDate).IsRequired();
                entity.Property(er => er.Status).IsRequired();
            });

            builder.Entity<NotificationMessage>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(n => n.CreatedAt)
                    .IsRequired();

                entity.Property(n => n.Type)
                    .IsRequired();

                entity.Property(n => n.UserId)
                    .IsRequired();

                entity.HasOne<Event>()
                    .WithMany()
                    .HasForeignKey(n => n.EventId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
