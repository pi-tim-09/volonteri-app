using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    /// <summary>
    /// Entity Framework DbContext for the VolonteriApp
    /// Manages database connections and entity configurations
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for entities
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Volunteer> Volunteers => Set<Volunteer>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<WebApp.Models.Application> Applications => Set<WebApp.Models.Application>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User hierarchy (TPT - Table Per Type)
            modelBuilder.Entity<User>()
                .UseTptMappingStrategy();

            // Configure Volunteer
            modelBuilder.Entity<Volunteer>(entity =>
            {
                entity.HasMany(v => v.Applications)
                    .WithOne(a => a.Volunteer)
                    .HasForeignKey(a => a.VolunteerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Organization
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasMany(o => o.Projects)
                    .WithOne(p => p.Organization)
                    .HasForeignKey(p => p.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(o => o.OrganizationName)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            // Configure Project
            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Description)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasMany(p => p.Applications)
                    .WithOne(a => a.Project)
                    .HasForeignKey(a => a.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.City);
            });

            // Configure Application - explicit typing to avoid System.Application conflict
            modelBuilder.Entity<WebApp.Models.Application>(entity =>
            {
                entity.HasIndex((WebApp.Models.Application a) => a.Status);
                entity.HasIndex((WebApp.Models.Application a) => new { a.VolunteerId, a.ProjectId })
                    .IsUnique();
            });

            // Configure User base class
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(u => u.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.HasIndex(u => u.Role);
            });
        }
    }
}
