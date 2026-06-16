using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Models;

public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
}
