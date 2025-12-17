namespace Lab1.Models;

public class Order
{
    public int Id { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

