using System.ComponentModel.DataAnnotations;

namespace DotnetInventoryApi.Models;

public sealed class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;
}