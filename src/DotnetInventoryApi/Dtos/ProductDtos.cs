using System.ComponentModel.DataAnnotations;

namespace DotnetInventoryApi.Dtos;

public sealed record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    decimal CostPrice,
    decimal SalePrice,
    int Stock,
    int MinimumStock,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int CategoryId,
    string CategoryName,
    int SupplierId,
    string SupplierName);

public sealed class CreateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumStock { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }
}

public sealed class UpdateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }
}