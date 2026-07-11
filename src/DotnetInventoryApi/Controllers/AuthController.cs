using System.Security.Claims;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using DotnetInventoryApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetInventoryApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    InventoryDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService jwtTokenService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new
            {
                message = "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Password is required."
            });
        }

        var emailAlreadyExists =
            await dbContext.Users.AnyAsync(
                user =>
                    user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (emailAlreadyExists)
        {
            return Conflict(new
            {
                message =
                    "An account with that email already exists."
            });
        }

        var user = new AppUser
        {
            FullName = fullName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = UserRoles.User,
            IsActive = true
        };

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                request.Password);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            CreateAuthResponse(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                currentUser =>
                    currentUser.NormalizedEmail ==
                    normalizedEmail,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var verificationResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    request.Password);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return Ok(CreateAuthResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>>
        GetCurrentUser(
            CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(currentUser =>
                currentUser.Id == userId &&
                currentUser.IsActive)
            .Select(currentUser =>
                new AuthUserResponse(
                    currentUser.Id,
                    currentUser.FullName,
                    currentUser.Email,
                    currentUser.Role))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    private AuthResponse CreateAuthResponse(
        AppUser user)
    {
        var token =
            jwtTokenService.CreateToken(user);

        return new AuthResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            new AuthUserResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Role));
    }

    private static string NormalizeEmail(
        string email)
    {
        return email
            .Trim()
            .ToUpperInvariant();
    }
}