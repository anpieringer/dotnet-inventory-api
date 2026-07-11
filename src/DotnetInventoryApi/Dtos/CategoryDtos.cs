using System.ComponentModel.DataAnnotations;

namespace DotnetInventoryApi.Dtos;

public sealed record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive);

public sealed class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public sealed class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}