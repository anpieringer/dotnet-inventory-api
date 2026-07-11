using System.ComponentModel.DataAnnotations;

namespace DotnetInventoryApi.Dtos;

public sealed record SupplierResponse(
    int Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive);

public sealed class CreateSupplierRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }
}

public sealed class UpdateSupplierRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}