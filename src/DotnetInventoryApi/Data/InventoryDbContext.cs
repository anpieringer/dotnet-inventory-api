using DotnetInventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetInventoryApi.Data;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(supplier => supplier.Id);

            entity.HasIndex(supplier => supplier.Name);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.HasIndex(product => product.Sku)
                .IsUnique();

            entity.Property(product => product.CostPrice)
                .HasPrecision(18, 2);

            entity.Property(product => product.SalePrice)
                .HasPrecision(18, 2);

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(product => product.Supplier)
                .WithMany(supplier => supplier.Products)
                .HasForeignKey(product => product.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.Property(user => user.FullName)
                .HasMaxLength(150);

            entity.Property(user => user.Email)
                .HasMaxLength(150);

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(150);

            entity.Property(user => user.Role)
                .HasMaxLength(30);
        });
    }
}