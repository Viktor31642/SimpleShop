using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleShop.Data;
using SimpleShop.Models;

namespace SimpleShop.Controllers
{
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
            {
                return View(model);
            }

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
                Items = selectedItems
                    .Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = products[i.ProductId].Price   
                    })
                    .ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }


        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
