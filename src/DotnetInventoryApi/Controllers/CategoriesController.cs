using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetInventoryApi.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(
    InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var alreadyExists = await dbContext.Categories
            .AnyAsync(
                category => category.Name == normalizedName,
                cancellationToken);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = "A category with that name already exists."
            });
        }

        var category = new Category
        {
            Name = normalizedName,
            Description = request.Description?.Trim()
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive);

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            return NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        var normalizedName = request.Name.Trim();

        var duplicateExists = await dbContext.Categories
            .AnyAsync(
                otherCategory =>
                    otherCategory.Id != id &&
                    otherCategory.Name == normalizedName,
                cancellationToken);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "A category with that name already exists."
            });
        }

        category.Name = normalizedName;
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            return NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        var hasProducts = await dbContext.Products
            .AnyAsync(
                product => product.CategoryId == id,
                cancellationToken);

        if (hasProducts)
        {
            return Conflict(new
            {
                message = "The category cannot be deleted because it has associated products."
            });
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}