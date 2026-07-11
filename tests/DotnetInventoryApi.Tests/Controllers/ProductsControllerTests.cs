using DotnetInventoryApi.Controllers;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetInventoryApi.Tests.Controllers;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_WithLowStockFilter_ReturnsLowStockProductsOrderedByName()
    {
        await using var dbContext = CreateDbContext();

        var (category, supplier) =
            await CreateActiveRelationsAsync(dbContext);

        dbContext.Products.AddRange(
            new Product
            {
                Sku = "TURBO-003",
                Name = "Zeta Turbo",
                CostPrice = 300000,
                SalePrice = 400000,
                Stock = 10,
                MinimumStock = 3,
                CategoryId = category.Id,
                SupplierId = supplier.Id
            },
            new Product
            {
                Sku = "TURBO-002",
                Name = "Beta Turbo",
                CostPrice = 300000,
                SalePrice = 400000,
                Stock = 3,
                MinimumStock = 3,
                CategoryId = category.Id,
                SupplierId = supplier.Id
            },
            new Product
            {
                Sku = "TURBO-001",
                Name = "Alpha Turbo",
                CostPrice = 300000,
                SalePrice = 400000,
                Stock = 1,
                MinimumStock = 3,
                CategoryId = category.Id,
                SupplierId = supplier.Id
            });

        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var result = await controller.GetAll(
            search: null,
            categoryId: null,
            supplierId: null,
            isActive: null,
            lowStock: true,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var products = Assert
            .IsAssignableFrom<IEnumerable<ProductResponse>>(
                okResult.Value)
            .ToList();

        Assert.Equal(2, products.Count);

        Assert.Equal(
            new[] { "Alpha Turbo", "Beta Turbo" },
            products.Select(product => product.Name).ToArray());

        Assert.All(
            products,
            product => Assert.True(
                product.Stock <= product.MinimumStock));
    }

    [Fact]
    public async Task GetAll_FiltersByCategorySupplierAndActiveStatus()
    {
        await using var dbContext = CreateDbContext();

        var categoryOne = new Category
        {
            Name = "Turbochargers"
        };

        var categoryTwo = new Category
        {
            Name = "Accessories"
        };

        var supplierOne = new Supplier
        {
            Name = "Supplier One"
        };

        var supplierTwo = new Supplier
        {
            Name = "Supplier Two"
        };

        dbContext.Categories.AddRange(
            categoryOne,
            categoryTwo);

        dbContext.Suppliers.AddRange(
            supplierOne,
            supplierTwo);

        await dbContext.SaveChangesAsync();

        dbContext.Products.AddRange(
            new Product
            {
                Sku = "MATCH-001",
                Name = "Active Match",
                CostPrice = 100000,
                SalePrice = 150000,
                Stock = 5,
                MinimumStock = 2,
                IsActive = true,
                CategoryId = categoryOne.Id,
                SupplierId = supplierOne.Id
            },
            new Product
            {
                Sku = "INACTIVE-001",
                Name = "Inactive Product",
                CostPrice = 100000,
                SalePrice = 150000,
                Stock = 5,
                MinimumStock = 2,
                IsActive = false,
                CategoryId = categoryOne.Id,
                SupplierId = supplierOne.Id
            },
            new Product
            {
                Sku = "CATEGORY-001",
                Name = "Wrong Category",
                CostPrice = 100000,
                SalePrice = 150000,
                Stock = 5,
                MinimumStock = 2,
                IsActive = true,
                CategoryId = categoryTwo.Id,
                SupplierId = supplierOne.Id
            },
            new Product
            {
                Sku = "SUPPLIER-001",
                Name = "Wrong Supplier",
                CostPrice = 100000,
                SalePrice = 150000,
                Stock = 5,
                MinimumStock = 2,
                IsActive = true,
                CategoryId = categoryOne.Id,
                SupplierId = supplierTwo.Id
            });

        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var result = await controller.GetAll(
            search: null,
            categoryId: categoryOne.Id,
            supplierId: supplierOne.Id,
            isActive: true,
            lowStock: false,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var products = Assert
            .IsAssignableFrom<IEnumerable<ProductResponse>>(
                okResult.Value)
            .ToList();

        var product = Assert.Single(products);

        Assert.Equal("MATCH-001", product.Sku);
        Assert.Equal("Active Match", product.Name);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsNormalizedProduct()
    {
        await using var dbContext = CreateDbContext();

        var (category, supplier) =
            await CreateActiveRelationsAsync(dbContext);

        var controller = new ProductsController(dbContext);

        var request = new CreateProductRequest
        {
            Sku = "  turbo-001  ",
            Name = "  Demo Turbocharger  ",
            Description = "  Inventory test product  ",
            CostPrice = 500000,
            SalePrice = 650000,
            Stock = 2,
            MinimumStock = 3,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(
                result.Result);

        Assert.Equal(
            nameof(ProductsController.GetById),
            createdResult.ActionName);

        var response = Assert.IsType<ProductResponse>(
            createdResult.Value);

        Assert.True(response.Id > 0);
        Assert.Equal("TURBO-001", response.Sku);
        Assert.Equal("Demo Turbocharger", response.Name);
        Assert.Equal(
            "Inventory test product",
            response.Description);

        Assert.Equal("Turbochargers", response.CategoryName);
        Assert.Equal("Turbo Supplier", response.SupplierName);
        Assert.True(response.IsActive);

        var savedProduct =
            await dbContext.Products.SingleAsync();

        Assert.Equal("TURBO-001", savedProduct.Sku);
        Assert.Equal("Demo Turbocharger", savedProduct.Name);
        Assert.Equal(
            "Inventory test product",
            savedProduct.Description);
    }

    [Fact]
    public async Task Create_WhenSkuAlreadyExists_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        var (category, supplier) =
            await CreateActiveRelationsAsync(dbContext);

        dbContext.Products.Add(new Product
        {
            Sku = "TURBO-001",
            Name = "Existing Turbo",
            CostPrice = 500000,
            SalePrice = 650000,
            Stock = 2,
            MinimumStock = 3,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        });

        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var request = new CreateProductRequest
        {
            Sku = "  turbo-001  ",
            Name = "Duplicate Turbo",
            CostPrice = 400000,
            SalePrice = 550000,
            Stock = 4,
            MinimumStock = 2,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };

        var result = await controller.Create(
            request,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(
            result.Result);

        Assert.Equal(
            1,
            await dbContext.Products.CountAsync());
    }

    [Fact]
    public async Task Create_WhenCategoryIsInactive_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();

        var category = new Category
        {
            Name = "Inactive Category",
            IsActive = false
        };

        var supplier = new Supplier
        {
            Name = "Active Supplier",
            IsActive = true
        };

        dbContext.Categories.Add(category);
        dbContext.Suppliers.Add(supplier);

        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var request = CreateValidRequest(
            category.Id,
            supplier.Id);

        var result = await controller.Create(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(
            result.Result);

        Assert.Empty(
            await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task Create_WhenSupplierIsInactive_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();

        var category = new Category
        {
            Name = "Active Category",
            IsActive = true
        };

        var supplier = new Supplier
        {
            Name = "Inactive Supplier",
            IsActive = false
        };

        dbContext.Categories.Add(category);
        dbContext.Suppliers.Add(supplier);

        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var request = CreateValidRequest(
            category.Id,
            supplier.Id);

        var result = await controller.Create(
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(
            result.Result);

        Assert.Empty(
            await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task Update_ModifiesProductAndReturnsOk()
    {
        await using var dbContext = CreateDbContext();

        var (category, supplier) =
            await CreateActiveRelationsAsync(dbContext);

        var product = new Product
        {
            Sku = "OLD-001",
            Name = "Original Product",
            Description = "Original description",
            CostPrice = 300000,
            SalePrice = 400000,
            Stock = 5,
            MinimumStock = 2,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var request = new UpdateProductRequest
        {
            Sku = "  updated-001  ",
            Name = "  Updated Product  ",
            Description = "  Updated description  ",
            CostPrice = 450000,
            SalePrice = 600000,
            Stock = 1,
            MinimumStock = 4,
            IsActive = false,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };

        var result = await controller.Update(
            product.Id,
            request,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<ProductResponse>(
            okResult.Value);

        Assert.Equal("UPDATED-001", response.Sku);
        Assert.Equal("Updated Product", response.Name);
        Assert.Equal(
            "Updated description",
            response.Description);

        Assert.Equal(450000, response.CostPrice);
        Assert.Equal(600000, response.SalePrice);
        Assert.Equal(1, response.Stock);
        Assert.Equal(4, response.MinimumStock);
        Assert.False(response.IsActive);
        Assert.NotNull(response.UpdatedAtUtc);

        var savedProduct =
            await dbContext.Products.SingleAsync();

        Assert.Equal("UPDATED-001", savedProduct.Sku);
        Assert.Equal("Updated Product", savedProduct.Name);
        Assert.False(savedProduct.IsActive);
        Assert.NotNull(savedProduct.UpdatedAtUtc);
    }

    [Fact]
    public async Task Delete_WhenProductExists_RemovesProduct()
    {
        await using var dbContext = CreateDbContext();

        var (category, supplier) =
            await CreateActiveRelationsAsync(dbContext);

        var product = new Product
        {
            Sku = "DELETE-001",
            Name = "Product To Delete",
            CostPrice = 250000,
            SalePrice = 350000,
            Stock = 2,
            MinimumStock = 1,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var controller = new ProductsController(dbContext);

        var result = await controller.Delete(
            product.Id,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        Assert.Empty(
            await dbContext.Products.ToListAsync());
    }

    private static async Task<(Category Category, Supplier Supplier)>
        CreateActiveRelationsAsync(
            InventoryDbContext dbContext)
    {
        var category = new Category
        {
            Name = "Turbochargers",
            Description = "Automotive turbochargers",
            IsActive = true
        };

        var supplier = new Supplier
        {
            Name = "Turbo Supplier",
            IsActive = true
        };

        dbContext.Categories.Add(category);
        dbContext.Suppliers.Add(supplier);

        await dbContext.SaveChangesAsync();

        return (category, supplier);
    }

    private static CreateProductRequest CreateValidRequest(
        int categoryId,
        int supplierId)
    {
        return new CreateProductRequest
        {
            Sku = "TURBO-001",
            Name = "Demo Turbocharger",
            Description = "Inventory test product",
            CostPrice = 500000,
            SalePrice = 650000,
            Stock = 2,
            MinimumStock = 3,
            CategoryId = categoryId,
            SupplierId = supplierId
        };
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