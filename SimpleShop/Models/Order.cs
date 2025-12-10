namespace SimpleShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
