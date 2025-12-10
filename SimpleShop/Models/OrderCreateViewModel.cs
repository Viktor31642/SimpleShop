namespace SimpleShop.Models
{
    public class ProductOrderItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }

        // чи вибрано товар
        public bool Selected { get; set; }

        // кількість (по замовчуванню 1)
        public int Quantity { get; set; } = 1;
    }

    public class OrderCreateViewModel
    {
        public List<ProductOrderItemViewModel> Items { get; set; } = [];
    }
}
