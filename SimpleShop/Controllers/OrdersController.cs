using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Data;
using SimpleShop.Models;

namespace SimpleShop.Controllers;

public class OrdersController(ShopContext context) : Controller
{
    private readonly ShopContext _context = context;

    // GET: Orders
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .ToListAsync();

        return View(orders);
    }

    // GET: Orders/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var products = await _context.Products.ToListAsync();

        var model = new OrderCreateViewModel
        {
            Items =
            [
                .. products.Select(p => new ProductOrderItemViewModel
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Selected = false,
                    Quantity = 1
                })
            ]
        };

        return View(model);
    }

    // POST: Orders/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var selectedItems = model.Items
            .Where(i => i.Quantity > 0)
            .ToList();

        if (selectedItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Будь ласка, оберіть хоча б один товар.");
            return View(model);
        }

        var productIds = selectedItems.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var order = new Order
        {
            OrderDate = DateTime.UtcNow,
            Items =
            [
                .. selectedItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = products[i.ProductId].Price
                })
            ]
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    // GET: Orders/Details/id
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // GET: Orders/Edit/id
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();


        var model = new OrderEditViewModel
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Items =
            [
                .. order.Items.Select(oi => new OrderItemEditViewModel
                {
                    OrderItemId = oi.Id,
                    ProductName = oi.Product.Name,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                })
            ]
        };

        return View(model);
    }

    // POST: Orders/Edit/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        order.OrderDate = model.OrderDate;

        foreach (var itemVm in model.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == itemVm.OrderItemId);
            if (item != null)
            {
                item.Quantity = itemVm.Quantity;
                item.UnitPrice = itemVm.UnitPrice;
            }
        }

        order.Items = [.. order.Items.Where(i => i.Quantity > 0)];

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    // GET: Orders/Delete/id
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // POST: Orders/Delete/id
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order != null)
        {
            _context.OrderItems.RemoveRange(order.Items);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
