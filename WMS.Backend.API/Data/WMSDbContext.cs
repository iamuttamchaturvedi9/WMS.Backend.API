using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using WMS.Backend.API.Models;
namespace WMS.Backend.API.Data;

public class WMSDbContext : DbContext
{
    public WMSDbContext(DbContextOptions<WMSDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerOrder> CustomerOrders { get; set; }
    public DbSet<OrderLineItem> OrderLineItems { get; set; }
    public DbSet<SKU> SKUs { get; set; }
    public DbSet<StockAllocation> StockAllocations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // SQLite-specific optimizations (if not configured in Program.cs)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=WMSStockAllocation.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CustomerOrder Configuration
        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.HasMany(e => e.LineItems)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Allocations)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Priority)
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            // SQLite doesn't need explicit index creation in most cases
            // but we can add them for better performance
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Priority, e.OrderDate });
        });

        // OrderLineItem Configuration
        modelBuilder.Entity<OrderLineItem>(entity =>
        {
            entity.HasKey(e => e.LineItemId);

            entity.HasMany(e => e.Allocations)
                .WithOne(e => e.LineItem)
                .HasForeignKey(e => e.LineItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductNumber);
            entity.HasIndex(e => e.Status);
        });

        // SKU Configuration
        modelBuilder.Entity<SKU>(entity =>
        {
            entity.HasKey(e => e.SkuId);

            entity.HasMany(e => e.Allocations)
                .WithOne(e => e.SKU)
                .HasForeignKey(e => e.SkuId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: SQLite check constraints work differently
            // This will be created but may need manual verification
            entity.HasCheckConstraint("CK_SKU_AvailableQuantity",
                "AvailableQuantity <= TotalQuantity");

            entity.HasIndex(e => e.ProductNumber);
            entity.HasIndex(e => e.WarehouseLocation);
            entity.HasIndex(e => new { e.ProductNumber, e.AvailableQuantity, e.IsLocationLocked });
        });

        // StockAllocation Configuration
        modelBuilder.Entity<StockAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.LineItemId);
            entity.HasIndex(e => e.SkuId);
            entity.HasIndex(e => new { e.OrderId, e.LineItemId });
        });

        // AuditLog Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.HasIndex(e => new { e.TableName, e.RecordId });
            entity.HasIndex(e => e.ChangedDate);
        });

        // Seed Initial Data (Optional)
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed SKUs
        modelBuilder.Entity<SKU>().HasData(
            new SKU
            {
                SkuId = "SKU001",
                ProductNumber = "P001",
                TotalQuantity = 100,
                AvailableQuantity = 100,
                WarehouseLocation = "A-01-01",
                IsLocationLocked = false,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            },
            new SKU
            {
                SkuId = "SKU002",
                ProductNumber = "P001",
                TotalQuantity = 50,
                AvailableQuantity = 50,
                WarehouseLocation = "A-01-02",
                IsLocationLocked = false,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            },
            new SKU
            {
                SkuId = "SKU003",
                ProductNumber = "P002",
                TotalQuantity = 75,
                AvailableQuantity = 75,
                WarehouseLocation = "B-02-01",
                IsLocationLocked = false,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            },
            new SKU
            {
                SkuId = "SKU004",
                ProductNumber = "P002",
                TotalQuantity = 25,
                AvailableQuantity = 25,
                WarehouseLocation = "B-02-02",
                IsLocationLocked = true,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            },
            new SKU
            {
                SkuId = "SKU005",
                ProductNumber = "P003",
                TotalQuantity = 200,
                AvailableQuantity = 200,
                WarehouseLocation = "C-03-01",
                IsLocationLocked = false,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is CustomerOrder order)
            {
                if (entry.State == EntityState.Added)
                    order.CreatedDate = DateTime.UtcNow;
                order.LastModifiedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is OrderLineItem lineItem)
            {
                if (entry.State == EntityState.Added)
                    lineItem.CreatedDate = DateTime.UtcNow;
                lineItem.LastModifiedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is SKU sku)
            {
                if (entry.State == EntityState.Added)
                    sku.CreatedDate = DateTime.UtcNow;
                sku.LastModifiedDate = DateTime.UtcNow;
            }
        }
    }
}