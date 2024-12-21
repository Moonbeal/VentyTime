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
        public DbSet<Registration> Registrations { get; set; } = null!;

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

                entity.HasOne(e => e.Organizer)
                    .WithMany(u => u.OrganizedEvents)
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Title)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Location)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.StartDate)
                    .IsRequired()
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.MaxAttendees)
                    .IsRequired();

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.OrganizerId)
                    .IsRequired();
            });

            builder.Entity<Registration>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.Event)
                    .WithMany(e => e.Registrations)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Registrations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(r => r.RegistrationDate)
                    .IsRequired();

                entity.Property(r => r.Status)
                    .HasMaxLength(50)
                    .IsRequired();
            });
        }
    }
}
