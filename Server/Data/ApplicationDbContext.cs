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

                entity.Property(e => e.Title)
                    .IsRequired()
                    .IsUnicode(true)
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .IsUnicode(true)
                    .HasMaxLength(1000);

                entity.Property(e => e.Location)
                    .IsRequired()
                    .IsUnicode(true)
                    .HasMaxLength(200);

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Organizer)
                    .WithMany(u => u.OrganizedEvents)
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Registrations)
                    .WithOne(r => r.Event)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Comments)
                    .WithOne(c => c.Event)
                    .HasForeignKey(c => c.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.Category)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.StartDate)
                    .IsRequired()
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.Property(e => e.EndDate)
                    .IsRequired()
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.Property(e => e.StartTime)
                    .IsRequired();

                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.MaxAttendees)
                    .IsRequired();

                entity.Property(e => e.CurrentCapacity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.OrganizerId)
                    .IsRequired();

                entity.Property(e => e.CreatorId)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            builder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Event)
                    .WithMany(e => e.Comments)
                    .HasForeignKey(c => c.EventId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.Property(c => c.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(c => c.CreatedAt)
                    .IsRequired()
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.Property(c => c.UpdatedAt)
                    .HasConversion(
                        v => v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

                entity.Property(c => c.IsEdited)
                    .IsRequired()
                    .HasDefaultValue(false);
            });

            builder.Entity<Registration>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => new { r.EventId, r.UserId, r.Status })
                    .IsUnique()
                    .HasFilter("[Status] != 'Cancelled'");

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(r => r.CreatedAt)
                    .IsRequired()
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.Property(r => r.UpdatedAt)
                    .HasConversion(
                        v => v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Registrations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Event)
                    .WithMany(e => e.Registrations)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
