using DotnetInventoryApi.Data;
using DotnetInventoryApi.Dtos;
using DotnetInventoryApi.Models;
using DotnetInventoryApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(
    InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Name,
                supplier.Email,
                supplier.Phone,
                supplier.Address,
                supplier.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(suppliers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Id == id)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Name,
                supplier.Email,
                supplier.Phone,
                supplier.Address,
                supplier.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
        {
            return NotFound(new
            {
                message = $"Supplier with ID {id} was not found."
            });
        }

        return Ok(supplier);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> Create(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var alreadyExists = await dbContext.Suppliers
            .AnyAsync(
                supplier => supplier.Name == normalizedName,
                cancellationToken);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = "A supplier with that name already exists."
            });
        }

        var supplier = new Supplier
        {
            Name = normalizedName,
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            Address = NormalizeOptional(request.Address)
        };

        dbContext.Suppliers.Add(supplier);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapResponse(supplier);

        return CreatedAtAction(
            nameof(GetById),
            new { id = supplier.Id },
            response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierResponse>> Update(
        int id,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .FirstOrDefaultAsync(
                supplier => supplier.Id == id,
                cancellationToken);

        if (supplier is null)
        {
            return NotFound(new
            {
                message = $"Supplier with ID {id} was not found."
            });
        }

        var normalizedName = request.Name.Trim();

        var duplicateExists = await dbContext.Suppliers
            .AnyAsync(
                otherSupplier =>
                    otherSupplier.Id != id &&
                    otherSupplier.Name == normalizedName,
                cancellationToken);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "A supplier with that name already exists."
            });
        }

        supplier.Name = normalizedName;
        supplier.Email = NormalizeOptional(request.Email);
        supplier.Phone = NormalizeOptional(request.Phone);
        supplier.Address = NormalizeOptional(request.Address);
        supplier.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapResponse(supplier));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers
            .FirstOrDefaultAsync(
                supplier => supplier.Id == id,
                cancellationToken);

        if (supplier is null)
        {
            return NotFound(new
            {
                message = $"Supplier with ID {id} was not found."
            });
        }

        var hasProducts = await dbContext.Products
            .AnyAsync(
                product => product.SupplierId == id,
                cancellationToken);

        if (hasProducts)
        {
            return Conflict(new
            {
                message =
                    "The supplier cannot be deleted because it has associated products."
            });
        }

        dbContext.Suppliers.Remove(supplier);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static SupplierResponse MapResponse(Supplier supplier)
    {
        return new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.IsActive);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}