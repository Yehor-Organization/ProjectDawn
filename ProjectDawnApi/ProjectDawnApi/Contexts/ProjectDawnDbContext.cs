 using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace ProjectDawnApi
{
    /// <summary>
    /// Represents the database session and provides access to the data models.
    /// EF Core uses this class to query and save data.
    /// </summary>
    public class ProjectDawnDbContext : DbContext
    {
        public ProjectDawnDbContext(DbContextOptions<ProjectDawnDbContext> options) : base(options) { }

        // Define the database tables (DbSets)
        public DbSet<PlayerDataModel> Players { get; set; }
        public DbSet<FarmDataModel> Farms { get; set; }
        public DbSet<PlacedObjectDataModel> PlacedObjects { get; set; }
        public DbSet<FarmVisitorDataModel> FarmVisitors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the relationships between the models

            // A FarmDataModel has one Owner (PlayerDataModel)
            modelBuilder.Entity<FarmDataModel>()
                .HasOne(f => f.Owner)
                .WithMany()
                .HasForeignKey(f => f.OwnerId)
                .IsRequired();

            // A FarmDataModel has many PlacedObjects
            modelBuilder.Entity<FarmDataModel>()
                .HasMany(f => f.PlacedObjects)
                .WithOne()
                .HasForeignKey(p => p.FarmId)
                .IsRequired();

            // Configure the many-to-many relationship between FarmDataModel and PlayerDataModel (for visitors)
            // using the FarmVisitorDataModel join table.
            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasKey(fv => new { fv.FarmId, fv.PlayerId }); // Composite key

            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasOne<FarmDataModel>()
                .WithMany(f => f.Visitors)
                .HasForeignKey(fv => fv.FarmId);

            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasOne(fv => fv.PlayerDataModel)
                .WithMany()
                .HasForeignKey(fv => fv.PlayerId);
        }
    }
}
