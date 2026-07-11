using System.ComponentModel.DataAnnotations;

namespace DotnetInventoryApi.Dtos;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthUserResponse(
    int Id,
    string FullName,
    string Email,
    string Role);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    AuthUserResponse User);