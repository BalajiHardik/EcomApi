using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcomApi.Data;
using EcomApi.Models;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly EcomDbContext _db;
    public OrderController(EcomDbContext db) => _db = db;

    // User places an order (COD only)
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null || role != "User") return Unauthorized();

        if (dto.Items == null || !dto.Items.Any()) return BadRequest("Order must contain items");

        var order = new Order
        {
            UserId = userId.Value,
            Status = "Pending",
            OrderDate = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        foreach (var item in dto.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null) return BadRequest($"ProductId {item.ProductId} not found");
            if (product.Stock < item.Quantity) return BadRequest($"Insufficient stock for {product.Name}");

            product.Stock -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { order.Id, order.Status });
    }

    // Anyone can view all orders
    [HttpGet("all")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .ToListAsync();

        return Ok(orders);
    }

    // Admin views all orders with filters & pagination
    [HttpGet]
    [Route("/api/admin/orders")]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin") return Forbid();

        var query = _db.Orders.Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        var totalItems = await query.CountAsync();

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { totalItems, page, pageSize, orders });
    }

    // Admin updates order status
    [HttpPut]
    [Route("/api/admin/orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin") return Forbid();

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(order);
    }
}

public class PlaceOrderDto
{
    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = "";
}
