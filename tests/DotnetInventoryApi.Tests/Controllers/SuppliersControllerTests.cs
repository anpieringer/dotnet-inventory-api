using DotnetInventoryApi.Controllers;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetInventoryApi.Tests.Controllers;

public sealed class SuppliersControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsSuppliersOrderedByName()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Suppliers.AddRange(
            new Supplier
            {
                Name = "Turbo Supplier"
            },
            new Supplier
            {
                Name = "Automotive Parts"
            },
            new Supplier
            {
                Name = "Diesel Components"
            });

        await dbContext.SaveChangesAsync();

        var controller = new SuppliersController(dbContext);

        var result = await controller.GetAll(
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var suppliers = Assert
            .IsAssignableFrom<IEnumerable<SupplierResponse>>(
                okResult.Value)
            .ToList();

        Assert.Equal(3, suppliers.Count);

        Assert.Equal(
            new[]
            {
                "Automotive Parts",
                "Diesel Components",
                "Turbo Supplier"
            },
            suppliers
                .Select(supplier => supplier.Name)
                .ToArray());
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsTrimmedSupplier()
    {
        await using var dbContext = CreateDbContext();

        var controller = new SuppliersController(dbContext);

        var request = new CreateSupplierRequest
        {
            Name = "  Turbo Supplier  ",
            Email = "  supplier@example.com  ",
            Phone = "  +56 9 1234 5678  ",
            Address = "  Santiago, Chile  "
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(
            result.Result);

        Assert.Equal(
            nameof(SuppliersController.GetById),
            createdResult.ActionName);

        var response = Assert.IsType<SupplierResponse>(
            createdResult.Value);

        Assert.True(response.Id > 0);
        Assert.Equal("Turbo Supplier", response.Name);
        Assert.Equal("supplier@example.com", response.Email);
        Assert.Equal("+56 9 1234 5678", response.Phone);
        Assert.Equal("Santiago, Chile", response.Address);
        Assert.True(response.IsActive);

        var savedSupplier =
            await dbContext.Suppliers.SingleAsync();

        Assert.Equal("Turbo Supplier", savedSupplier.Name);
        Assert.Equal(
            "supplier@example.com",
            savedSupplier.Email);
        Assert.Equal(
            "+56 9 1234 5678",
            savedSupplier.Phone);
        Assert.Equal(
            "Santiago, Chile",
            savedSupplier.Address);
    }

    [Fact]
    public async Task Create_WithWhitespaceOptionalValues_SavesNull()
    {
        await using var dbContext = CreateDbContext();

        var controller = new SuppliersController(dbContext);

        var request = new CreateSupplierRequest
        {
            Name = "Demo Supplier",
            Email = "   ",
            Phone = "   ",
            Address = "   "
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(
            result.Result);

        var response = Assert.IsType<SupplierResponse>(
            createdResult.Value);

        Assert.Null(response.Email);
        Assert.Null(response.Phone);
        Assert.Null(response.Address);

        var savedSupplier =
            await dbContext.Suppliers.SingleAsync();

        Assert.Null(savedSupplier.Email);
        Assert.Null(savedSupplier.Phone);
        Assert.Null(savedSupplier.Address);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExists_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Suppliers.Add(new Supplier
        {
            Name = "Turbo Supplier"
        });

        await dbContext.SaveChangesAsync();

        var controller = new SuppliersController(dbContext);

        var request = new CreateSupplierRequest
        {
            Name = "  Turbo Supplier  ",
            Email = "duplicate@example.com"
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);

        Assert.Equal(
            1,
            await dbContext.Suppliers.CountAsync());
    }

    [Fact]
    public async Task Update_ModifiesSupplierAndReturnsOk()
    {
        await using var dbContext = CreateDbContext();

        var supplier = new Supplier
        {
            Name = "Original Supplier",
            Email = "original@example.com",
            Phone = "+56 9 1111 1111",
            Address = "Original address"
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync();

        var controller = new SuppliersController(dbContext);

        var request = new UpdateSupplierRequest
        {
            Name = "  Updated Supplier  ",
            Email = "  updated@example.com  ",
            Phone = "  +56 9 2222 2222  ",
            Address = "  Updated address  ",
            IsActive = false
        };

        var result = await controller.Update(
            supplier.Id,
            request,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<SupplierResponse>(
            okResult.Value);

        Assert.Equal("Updated Supplier", response.Name);
        Assert.Equal("updated@example.com", response.Email);
        Assert.Equal("+56 9 2222 2222", response.Phone);
        Assert.Equal("Updated address", response.Address);
        Assert.False(response.IsActive);

        var savedSupplier =
            await dbContext.Suppliers.SingleAsync();

        Assert.Equal("Updated Supplier", savedSupplier.Name);
        Assert.False(savedSupplier.IsActive);
    }

    [Fact]
    public async Task Delete_WhenSupplierHasProducts_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        var category = new Category
        {
            Name = "Turbochargers",
            Description = "Automotive turbochargers"
        };

        var supplier = new Supplier
        {
            Name = "Turbo Supplier"
        };

        dbContext.Categories.Add(category);
        dbContext.Suppliers.Add(supplier);

        await dbContext.SaveChangesAsync();

        dbContext.Products.Add(new Product
        {
            Sku = "TURBO-001",
            Name = "Demo Turbocharger",
            Description = "Test product",
            CostPrice = 500000,
            SalePrice = 650000,
            Stock = 2,
            MinimumStock = 3,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        });

        await dbContext.SaveChangesAsync();

        var controller = new SuppliersController(dbContext);

        var result = await controller.Delete(
            supplier.Id,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);

        Assert.True(
            await dbContext.Suppliers.AnyAsync(
                currentSupplier =>
                    currentSupplier.Id == supplier.Id));
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(
                    $"InventoryTests-{Guid.NewGuid()}")
                .Options;

        return new InventoryDbContext(options);
    }
}