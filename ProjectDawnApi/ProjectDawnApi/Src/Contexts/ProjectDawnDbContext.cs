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
            // Farm → Owner (many-to-many)
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
                .WithOne(o => o.Farm)
                .HasForeignKey(o => o.FarmId)
                .IsRequired();

            // -----------------------------
            // FarmSession (VisitorDM)
            // -----------------------------
            modelBuilder.Entity<VisitorDM>()
                .HasKey(v => v.Id); // ✅ SINGLE PRIMARY KEY

            modelBuilder.Entity<VisitorDM>()
                .HasIndex(v => v.PlayerId)
                .IsUnique(); // ✅ ONE ACTIVE SESSION PER PLAYER

            modelBuilder.Entity<VisitorDM>()
                .HasOne(v => v.Farm)
                .WithMany(f => f.Visitors)
                .HasForeignKey(v => v.FarmId)
                .IsRequired();

            modelBuilder.Entity<VisitorDM>()
                .HasOne(v => v.PlayerDataModel)
                .WithMany()
                .HasForeignKey(v => v.PlayerId)
                .IsRequired();

            // -----------------------------
            // Player → Inventory (1:1)
            // -----------------------------
            modelBuilder.Entity<PlayerDM>()
                .HasOne(p => p.Inventory)
                .WithOne(i => i.Player)
                .HasForeignKey<InventoryDM>(i => i.PlayerId)
                .IsRequired();

            // -----------------------------
            // Inventory → Items (1:M)
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
                .OwnsOne(o => o.Transformation);

            modelBuilder.Entity<VisitorDM>()
                .OwnsOne(v => v.Transformation);
        }
    }
}