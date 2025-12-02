using Microsoft.EntityFrameworkCore;

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
        public DbSet<InventoryItemDataModel> InventoryItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // Player → Inventory (one-to-many)
            // -----------------------------
            modelBuilder.Entity<PlayerDataModel>()
                .HasMany(p => p.Inventory)
                .WithOne(i => i.Player)
                .HasForeignKey(i => i.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------------
            // Farm → Owner (many-to-one)
            // -----------------------------
            modelBuilder.Entity<FarmDataModel>()
                .HasOne(f => f.Owner)
                .WithMany()
                .HasForeignKey(f => f.OwnerId)
                .IsRequired();

            // -----------------------------
            // Farm → PlacedObjects (one-to-many)
            // -----------------------------
            modelBuilder.Entity<FarmDataModel>()
                .HasMany(f => f.PlacedObjects)
                .WithOne()
                .HasForeignKey(p => p.FarmId)
                .IsRequired();

            // -----------------------------
            // Farm ↔ Player (many-to-many) via FarmVisitorDataModel
            // -----------------------------
            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasKey(fv => new { fv.FarmId, fv.PlayerId });

            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasOne(fv => fv.Farm)
                .WithMany(f => f.Visitors)
                .HasForeignKey(fv => fv.FarmId);

            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasOne(fv => fv.PlayerDataModel)
                .WithMany()
                .HasForeignKey(fv => fv.PlayerId);

            // -----------------------------
            // Owned types
            // -----------------------------
            modelBuilder.Entity<PlacedObjectDataModel>()
                .OwnsOne(p => p.Transformation);

            modelBuilder.Entity<FarmVisitorDataModel>()
                .OwnsOne(v => v.Transformation);
        }
    }
}
