using System.Security.Claims;
using DotnetInventoryApi.Controllers;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using DotnetInventoryApi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetInventoryApi.Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_CreatesUserAndStoresHashedPassword()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<AppUser>();

        var controller = CreateController(
            dbContext,
            passwordHasher);

        var request = new RegisterRequest
        {
            FullName = "  Inventory User  ",
            Email = "  USER@INVENTORY.LOCAL  ",
            Password = "Inventory123!"
        };

        var result = await controller.Register(
            request,
            CancellationToken.None);

        var createdResult = Assert.IsType<ObjectResult>(
            result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            createdResult.StatusCode);

        var response = Assert.IsType<AuthResponse>(
            createdResult.Value);

        Assert.False(
            string.IsNullOrWhiteSpace(
                response.AccessToken));

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("Inventory User", response.User.FullName);
        Assert.Equal(
            "user@inventory.local",
            response.User.Email);
        Assert.Equal(UserRoles.User, response.User.Role);

        var savedUser =
            await dbContext.Users.SingleAsync();

        Assert.Equal(
            "Inventory User",
            savedUser.FullName);

        Assert.Equal(
            "user@inventory.local",
            savedUser.Email);

        Assert.Equal(
            "USER@INVENTORY.LOCAL",
            savedUser.NormalizedEmail);

        Assert.Equal(
            UserRoles.User,
            savedUser.Role);

        Assert.True(savedUser.IsActive);

        Assert.NotEqual(
            request.Password,
            savedUser.PasswordHash);

        var passwordVerification =
            passwordHasher.VerifyHashedPassword(
                savedUser,
                savedUser.PasswordHash,
                request.Password);

        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordVerification);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<AppUser>();

        var controller = CreateController(
            dbContext,
            passwordHasher);

        var firstRequest = new RegisterRequest
        {
            FullName = "First User",
            Email = "user@inventory.local",
            Password = "Inventory123!"
        };

        await controller.Register(
            firstRequest,
            CancellationToken.None);

        var duplicateRequest = new RegisterRequest
        {
            FullName = "Duplicate User",
            Email = "  USER@INVENTORY.LOCAL  ",
            Password = "AnotherPassword123!"
        };

        var result = await controller.Register(
            duplicateRequest,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(
            result.Result);

        Assert.Equal(
            1,
            await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<AppUser>();

        var user = await CreateUserAsync(
            dbContext,
            passwordHasher,
            isActive: true);

        var controller = CreateController(
            dbContext,
            passwordHasher);

        var request = new LoginRequest
        {
            Email = "  USER@INVENTORY.LOCAL  ",
            Password = "Inventory123!"
        };

        var result = await controller.Login(
            request,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<AuthResponse>(
            okResult.Value);

        Assert.False(
            string.IsNullOrWhiteSpace(
                response.AccessToken));

        Assert.Equal("Bearer", response.TokenType);

        Assert.True(
            response.ExpiresAtUtc > DateTime.UtcNow);

        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.FullName, response.User.FullName);
        Assert.Equal(user.Email, response.User.Email);
        Assert.Equal(UserRoles.User, response.User.Role);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<AppUser>();

        await CreateUserAsync(
            dbContext,
            passwordHasher,
            isActive: true);

        var controller = CreateController(
            dbContext,
            passwordHasher);

        var request = new LoginRequest
        {
            Email = "user@inventory.local",
            Password = "IncorrectPassword123!"
        };

        var result = await controller.Login(
            request,
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(
            result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<AppUser>();

        var user = await CreateUserAsync(
            dbContext,
            passwordHasher,
            isActive: true);

        var controller = CreateController(
            dbContext,
            passwordHasher);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    user.Id.ToString())
                            },
                            "TestAuthentication"))
                }
            };

        var result = await controller.GetCurrentUser(
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response =
            Assert.IsType<AuthUserResponse>(
                okResult.Value);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.FullName, response.FullName);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Role, response.Role);
    }

    private static AuthController CreateController(
        InventoryDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher)
    {
        var jwtOptions = Options.Create(
            new JwtOptions
            {
                Issuer = "DotnetInventoryApi.Tests",
                Audience =
                    "DotnetInventoryApi.Tests.Client",
                Key = Convert.ToBase64String(
                    new byte[64]),
                ExpirationMinutes = 60
            });

        var tokenService =
            new JwtTokenService(jwtOptions);

        return new AuthController(
            dbContext,
            passwordHasher,
            tokenService);
    }

    private static async Task<AppUser> CreateUserAsync(
        InventoryDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        bool isActive)
    {
        var user = new AppUser
        {
            FullName = "Inventory User",
            Email = "user@inventory.local",
            NormalizedEmail =
                "USER@INVENTORY.LOCAL",
            Role = UserRoles.User,
            IsActive = isActive
        };

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                "Inventory123!");

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        return user;
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(
                    $"InventoryAuthTests-{Guid.NewGuid()}")
                .Options;

        return new InventoryDbContext(options);
    }
}