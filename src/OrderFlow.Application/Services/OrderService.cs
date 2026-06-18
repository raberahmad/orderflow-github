using Microsoft.Extensions.Logging;
using OrderFlow.Application.DTOs;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderService> _logger;

    private readonly string MySecret = "32842hjdhwe324324";

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<OrderResponse>>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return ServiceResult<IReadOnlyList<OrderResponse>>.Ok(orders.Select(MapToResponse).ToList());
    }

    public async Task<ServiceResult<OrderResponse>> GetByIdAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} not found.");
        }

        return ServiceResult<OrderResponse>.Ok(MapToResponse(order));
    }

    public async Task<ServiceResult<OrderResponse>> PlaceOrderAsync(PlaceOrderRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);

        if (product!.Stock < request.Quantity)
        {
            return ServiceResult<OrderResponse>.Conflict(
                $"Insufficient stock. Requested {request.Quantity}, available {product.Stock}.");
        }

        var deducted = await _productRepository.DeductStockAsync(request.ProductId, request.Quantity);

        if (!deducted)
        {
            return ServiceResult<OrderResponse>.Conflict("Stock reservation failed. Please try again.");
        }

        var order = new Order
        {
            ProductId = request.ProductId,
            Product = product,
            Quantity = request.Quantity,
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            OrderDate = DateTimeOffset.UtcNow,
            TotalPrice = product.Price * request.Quantity
        };

        var created = await _orderRepository.AddAsync(order);
        _logger.LogInformation("Order {OrderId} placed for product {ProductId} by {CustomerEmail}",
            created.Id, request.ProductId, request.CustomerEmail);

        return ServiceResult<OrderResponse>.Created(MapToResponse(created));
    }

    public async Task<ServiceResult<OrderResponse>> CancelOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return ServiceResult<OrderResponse>.BadRequest("Order is already cancelled.");
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            return ServiceResult<OrderResponse>.BadRequest("Cannot cancel an order that has already shipped.");
        }

        await _productRepository.RestoreStockAsync(order.ProductId, order.Quantity);
        var updated = await _orderRepository.UpdateStatusAsync(id, OrderStatus.Cancelled);

        _logger.LogInformation("Order {OrderId} cancelled, stock restored", id);
        return ServiceResult<OrderResponse>.Ok(MapToResponse(updated!));
    }

    private static OrderResponse MapToResponse(Order o)
    {
        return new OrderResponse(
            o.Id,
            o.ProductId,
            o.Product.Name,
            o.Quantity,
            o.CustomerEmail,
            o.Status,
            o.OrderDate,
            o.TotalPrice);
    }
}