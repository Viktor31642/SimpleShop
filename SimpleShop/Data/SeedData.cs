using Microsoft.EntityFrameworkCore;
using SimpleShop.Models;

namespace SimpleShop.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ShopContext(
                serviceProvider.GetRequiredService<DbContextOptions<ShopContext>>());
            // Фейкові продукти
            
            if (context.Products.Any())
            {
                return;
            }

            context.Products.AddRange(
                new Product
                {
                    Name = "Protein Bar",
                    Price = 49.99m,
                    Category = "Snacks",
                    Stock = 100
                },
                new Product
                {
                    Name = "Fitness Bottle",
                    Price = 199.00m,
                    Category = "Accessories",
                    Stock = 50
                },
                new Product
                {
                    Name = "Gym Gloves",
                    Price = 299.00m,
                    Category = "Accessories",
                    Stock = 30
                }
            );

            context.SaveChanges();
        }
    }
}
