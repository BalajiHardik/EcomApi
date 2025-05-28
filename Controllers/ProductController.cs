using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcomApi.Data;
using EcomApi.Models;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly EcomDbContext _db;
    public ProductController(EcomDbContext db) => _db = db;

    // Public - product list with filtering & pagination
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalItems = await query.CountAsync();

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { totalItems, page, pageSize, products });
    }

    // Admin only CRUD actions
    [HttpPost]
    [Route("/api/admin/products")]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut]
    [Route("/api/admin/products/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = product.Name;
        existing.Category = product.Category;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.Description = product.Description;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete]
    [Route("/api/admin/products/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        _db.Products.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok("Deleted");
    }

    
}
