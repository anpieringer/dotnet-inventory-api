using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetInventoryApi.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] bool? isActive,
        [FromQuery] bool lowStock,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";

            query = query.Where(product =>
                EF.Functions.ILike(product.Sku, searchPattern) ||
                EF.Functions.ILike(product.Name, searchPattern));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == categoryId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(product =>
                product.SupplierId == supplierId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(product =>
                product.IsActive == isActive.Value);
        }

        if (lowStock)
        {
            query = query.Where(product =>
                product.Stock <= product.MinimumStock);
        }

        var products = await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.CostPrice,
                product.SalePrice,
                product.Stock,
                product.MinimumStock,
                product.IsActive,
                product.CreatedAtUtc,
                product.UpdatedAtUtc,
                product.CategoryId,
                product.Category.Name,
                product.SupplierId,
                product.Supplier.Name))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await GetResponseByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var skuExists = await dbContext.Products
            .AnyAsync(
                product => product.Sku == normalizedSku,
                cancellationToken);

        if (skuExists)
        {
            return Conflict(new
            {
                message = "A product with that SKU already exists."
            });
        }

        var categoryExists = await dbContext.Categories
            .AnyAsync(
                category =>
                    category.Id == request.CategoryId &&
                    category.IsActive,
                cancellationToken);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist or is inactive."
            });
        }

        var supplierExists = await dbContext.Suppliers
            .AnyAsync(
                supplier =>
                    supplier.Id == request.SupplierId &&
                    supplier.IsActive,
                cancellationToken);

        if (!supplierExists)
        {
            return BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            });
        }

        var product = new Product
        {
            Sku = normalizedSku,
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            Stock = request.Stock,
            MinimumStock = request.MinimumStock,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetResponseByIdAsync(
            product.Id,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var duplicateSku = await dbContext.Products
            .AnyAsync(
                otherProduct =>
                    otherProduct.Id != id &&
                    otherProduct.Sku == normalizedSku,
                cancellationToken);

        if (duplicateSku)
        {
            return Conflict(new
            {
                message = "A product with that SKU already exists."
            });
        }

        var categoryExists = await dbContext.Categories
            .AnyAsync(
                category =>
                    category.Id == request.CategoryId &&
                    category.IsActive,
                cancellationToken);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist or is inactive."
            });
        }

        var supplierExists = await dbContext.Suppliers
            .AnyAsync(
                supplier =>
                    supplier.Id == request.SupplierId &&
                    supplier.IsActive,
                cancellationToken);

        if (!supplierExists)
        {
            return BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            });
        }

        product.Sku = normalizedSku;
        product.Name = normalizedName;
        product.Description = NormalizeOptional(request.Description);
        product.CostPrice = request.CostPrice;
        product.SalePrice = request.SalePrice;
        product.Stock = request.Stock;
        product.MinimumStock = request.MinimumStock;
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetResponseByIdAsync(
            product.Id,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Product with ID {id} was not found."
            });
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<ProductResponse?> GetResponseByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.CostPrice,
                product.SalePrice,
                product.Stock,
                product.MinimumStock,
                product.IsActive,
                product.CreatedAtUtc,
                product.UpdatedAtUtc,
                product.CategoryId,
                product.Category.Name,
                product.SupplierId,
                product.Supplier.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}