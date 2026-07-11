using DotnetInventoryApi.Controllers;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetInventoryApi.Tests.Controllers;

public sealed class CategoriesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsCategoriesOrderedByName()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Categories.AddRange(
            new Category
            {
                Name = "Turbochargers",
                Description = "Turbocharger products"
            },
            new Category
            {
                Name = "Accessories",
                Description = "Inventory accessories"
            },
            new Category
            {
                Name = "Engines",
                Description = "Diesel engines"
            });

        await dbContext.SaveChangesAsync();

        var controller = new CategoriesController(dbContext);

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var categories = Assert
            .IsAssignableFrom<IEnumerable<CategoryResponse>>(okResult.Value)
            .ToList();

        Assert.Equal(3, categories.Count);

        Assert.Equal(
            new[] { "Accessories", "Engines", "Turbochargers" },
            categories.Select(category => category.Name).ToArray());
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsTrimmedCategory()
    {
        await using var dbContext = CreateDbContext();

        var controller = new CategoriesController(dbContext);

        var request = new CreateCategoryRequest
        {
            Name = "  Turbochargers  ",
            Description = "  Automotive turbocharger products  "
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(
            result.Result);

        Assert.Equal(
            nameof(CategoriesController.GetById),
            createdResult.ActionName);

        var response = Assert.IsType<CategoryResponse>(
            createdResult.Value);

        Assert.True(response.Id > 0);
        Assert.Equal("Turbochargers", response.Name);
        Assert.Equal(
            "Automotive turbocharger products",
            response.Description);

        var savedCategory = await dbContext.Categories.SingleAsync();

        Assert.Equal("Turbochargers", savedCategory.Name);
        Assert.Equal(
            "Automotive turbocharger products",
            savedCategory.Description);
        Assert.True(savedCategory.IsActive);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExists_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Categories.Add(new Category
        {
            Name = "Turbochargers",
            Description = "Existing category"
        });

        await dbContext.SaveChangesAsync();

        var controller = new CategoriesController(dbContext);

        var request = new CreateCategoryRequest
        {
            Name = "  Turbochargers  ",
            Description = "Duplicate category"
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);

        Assert.Equal(
            1,
            await dbContext.Categories.CountAsync());
    }

    [Fact]
    public async Task GetById_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();

        var controller = new CategoriesController(dbContext);

        var result = await controller.GetById(
            999,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WhenCategoryHasNoProducts_ReturnsNoContent()
    {
        await using var dbContext = CreateDbContext();

        var category = new Category
        {
            Name = "Accessories",
            Description = "Inventory accessories"
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var controller = new CategoriesController(dbContext);

        var result = await controller.Delete(
            category.Id,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        Assert.Empty(await dbContext.Categories.ToListAsync());
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(
                $"InventoryTests-{Guid.NewGuid()}")
            .Options;

        return new InventoryDbContext(options);
    }
}