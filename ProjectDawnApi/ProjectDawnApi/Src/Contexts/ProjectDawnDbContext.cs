using Microsoft.EntityFrameworkCore;
using ProjectDawnApi.Src.DataClasses.Visitor;

namespace ProjectDawnApi
{
    /// <summary>
    /// Represents the database session and provides access to the data models.
    /// EF Core uses this class to query and save data.
    /// </summary>
    public class ProjectDawnDbContext : DbContext
    {
        public ProjectDawnDbContext(DbContextOptions<ProjectDawnDbContext> options)
            : base(options) { }

        public DbSet<FarmDM> Farms { get; set; }

        public DbSet<VisitorDM> FarmVisitors { get; set; }

        public DbSet<InventoryDM> Inventories { get; set; }

        public DbSet<InventoryItemDM> InventoryItems { get; set; }

        public DbSet<ObjectDM> PlacedObjects { get; set; }

        // -----------------------------
        // DbSets (tables)
        // -----------------------------
        public DbSet<PlayerDM> Players { get; set; }

        public DbSet<RefreshTokenDM> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefreshTokenDM>()
                .HasOne(r => r.Player)
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(r => r.PlayerId);

            // -----------------------------
            // Farm → Owner (many-to-one)
            // -----------------------------
            modelBuilder.Entity<FarmDM>()
                .HasMany(f => f.Owners)
                .WithMany(p => p.Farms)
                .UsingEntity(j =>
                    j.ToTable("FarmOwners"));

            // -----------------------------
            // Farm → PlacedObjects (one-to-many)
            // -----------------------------
            modelBuilder.Entity<FarmDM>()
                .HasMany(f => f.Objects)
                .WithOne()
                .HasForeignKey(p => p.FarmId)
                .IsRequired();

            // -----------------------------
            // Farm ↔ Player (many-to-many) via FarmVisitor
            // -----------------------------
            modelBuilder.Entity<VisitorDM>()
                .HasKey(fv => new { fv.FarmId, fv.PlayerId });

            modelBuilder.Entity<VisitorDM>()
                .HasOne(fv => fv.Farm)
                .WithMany(f => f.Visitors)
                .HasForeignKey(fv => fv.FarmId)
                .IsRequired();

            modelBuilder.Entity<VisitorDM>()
                .HasOne(fv => fv.PlayerDataModel)
                .WithMany()
                .HasForeignKey(fv => fv.PlayerId)
                .IsRequired();

            // -----------------------------
            // Player → Inventory (one-to-one)
            // -----------------------------
            modelBuilder.Entity<PlayerDM>()
                .HasOne(p => p.Inventory)
                .WithOne(i => i.Player)
                .HasForeignKey<InventoryDM>(i => i.PlayerId)
                .IsRequired();

            // -----------------------------
            // Inventory → InventoryItems (one-to-many)
            // -----------------------------
            modelBuilder.Entity<InventoryItemDM>()
                .HasOne(ii => ii.Inventory)
                .WithMany(inv => inv.Items)
                .HasForeignKey(ii => ii.InventoryId)
                .IsRequired();

            // -----------------------------
            // Owned types
            // -----------------------------
            modelBuilder.Entity<ObjectDM>()
                .OwnsOne(p => p.Transformation);

            modelBuilder.Entity<VisitorDM>()
                .OwnsOne(v => v.Transformation);
        }
    }
}