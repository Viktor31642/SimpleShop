namespace SimpleShop.Models
{
    public class ProductOrderItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }

        public bool Selected { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public class OrderCreateViewModel
    {
        public List<ProductOrderItemViewModel> Items { get; set; } = [];
    }
}
