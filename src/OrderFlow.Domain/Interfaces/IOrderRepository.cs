using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Models;

namespace OrderFlow.Domain.Interfaces;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order> AddAsync(Order order);
    Task<Order?> UpdateStatusAsync(int id, OrderStatus status);
}
