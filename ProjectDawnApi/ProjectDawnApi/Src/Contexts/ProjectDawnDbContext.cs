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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // Farm → Owner (many-to-one)
            // -----------------------------
            modelBuilder.Entity<FarmDataModel>()
                .HasOne(f => f.Owner)
                .WithMany() // if you later add ICollection<FarmDataModel> OwnedFarms to Player, replace this
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
                .WithMany(f => f.Visitors) // ✅ wires FarmDataModel.Visitors:contentReference[oaicite:0]{index=0}
                .HasForeignKey(fv => fv.FarmId);

            modelBuilder.Entity<FarmVisitorDataModel>()
                .HasOne(fv => fv.PlayerDataModel)
                .WithMany() // if you want reverse navigation, add ICollection<FarmVisitorDataModel> Visits to Player:contentReference[oaicite:1]{index=1}:contentReference[oaicite:2]{index=2}
                .HasForeignKey(fv => fv.PlayerId);

            // -----------------------------
            // Owned type: TransformationDataModel
            // -----------------------------
            modelBuilder.Entity<PlacedObjectDataModel>()
                .OwnsOne(p => p.Transformation); // :contentReference[oaicite:3]{index=3}:contentReference[oaicite:4]{index=4}

            modelBuilder.Entity<FarmVisitorDataModel>()
                .OwnsOne(v => v.Transformation); // :contentReference[oaicite:5]{index=5}:contentReference[oaicite:6]{index=6}
        }
    }
}
