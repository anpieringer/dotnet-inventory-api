using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotnetInventoryApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotnetInventoryApi.Security;

public sealed record GeneratedJwtToken(
    string AccessToken,
    DateTime ExpiresAtUtc);

public sealed class JwtTokenService(
    IOptions<JwtOptions> options)
{
    public GeneratedJwtToken CreateToken(AppUser user)
    {
        var jwtOptions = options.Value;

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(
            jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                ClaimTypes.Role,
                user.Role),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        var keyBytes = Convert.FromBase64String(
            jwtOptions.Key);

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new GeneratedJwtToken(
            accessToken,
            expiresAtUtc);
    }
}