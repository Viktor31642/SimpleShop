namespace SimpleShop.Models
{
    public class OrderItemEditViewModel
    {
        public int OrderItemId { get; set; }

        public string ProductName { get; set; } = null!;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }

    public class OrderEditViewModel
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        public List<OrderItemEditViewModel> Items { get; set; } = [];
    }
}
