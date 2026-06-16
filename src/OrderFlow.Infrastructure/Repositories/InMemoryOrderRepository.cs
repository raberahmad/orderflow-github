using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;

namespace OrderFlow.Infrastructure.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = [];
    private int _nextId = 1;

    public Task<IReadOnlyList<Order>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Order>>(_orders.AsReadOnly());
    }

    public Task<Order?> GetByIdAsync(int id)
    {
        return Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
    }

    public Task<Order> AddAsync(Order order)
    {
        order.Id = _nextId++;
        _orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<Order?> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);

        if (order is null)
        {
            return Task.FromResult<Order?>(null);
        }

        order.Status = status;
        return Task.FromResult<Order?>(order);
    }
}
