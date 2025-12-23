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
        // 1) Беремо тільки те, що реально вибрали
        var selectedItems = model.Items
            .Where(i => i.Selected)
            .ToList();

        if (selectedItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Будь ласка, оберіть хоча б один товар.");
            await RehydrateProductsAsync(model);
            return View(model);
        }

        // 2) Перевірка кількостей
        foreach (var i in selectedItems)
        {
            if (i.Quantity <= 0)
                ModelState.AddModelError(string.Empty, $"Кількість має бути більшою за 0 (товар Id={i.ProductId}).");
        }

        if (!ModelState.IsValid)
        {
            await RehydrateProductsAsync(model);
            return View(model);
        }

        // 3) Підтягуємо товари з БД
        var productIds = selectedItems.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var productsById = products.ToDictionary(p => p.Id);

        // 4) Перевіряємо склад
        foreach (var i in selectedItems)
        {
            if (!productsById.TryGetValue(i.ProductId, out var product))
            {
                ModelState.AddModelError(string.Empty, $"Товар (Id={i.ProductId}) не знайдено.");
                continue;
            }

            if (product.Stock < i.Quantity)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"{product.Name}: на складі {product.Stock}, ви замовили {i.Quantity}."
                );
            }
        }

        if (!ModelState.IsValid)
        {
            await RehydrateProductsAsync(model, productsById);
            return View(model);
        }

        // 5) Створення замовлення + списання складу (в транзакції)
        await using var tx = await _context.Database.BeginTransactionAsync();

        var order = new Order
        {
            OrderDate = DateTime.UtcNow,
            Items = []
        };

        foreach (var i in selectedItems)
        {
            var product = productsById[i.ProductId];

            // списуємо склад
            product.Stock -= i.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = i.Quantity,
                UnitPrice = product.Price // ціна тільки з БД
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    private async Task RehydrateProductsAsync(
        OrderCreateViewModel model,
        Dictionary<int, Product>? productsById = null)
    {
        productsById ??= await _context.Products.ToDictionaryAsync(p => p.Id);

        foreach (var item in model.Items)
        {
            if (productsById.TryGetValue(item.ProductId, out var p))
            {
                item.Name = p.Name;
                item.Price = p.Price;
            }
        }
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
