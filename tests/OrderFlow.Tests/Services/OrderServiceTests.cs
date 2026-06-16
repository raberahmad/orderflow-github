using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderFlow.Application.DTOs;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;
using Xunit;

namespace OrderFlow.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepositoryMock   = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _sut = new OrderService(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenProductNotFound_Returns404()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var request = new PlaceOrderRequest(99, 1, "customer@example.com");
        var result = await _sut.PlaceOrderAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenInsufficientStock_Returns409()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 2, CreatedAt = DateTimeOffset.UtcNow };
        _productRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var request = new PlaceOrderRequest(1, 5, "customer@example.com");
        var result = await _sut.PlaceOrderAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("Insufficient stock", result.Error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenStockDeductionFails_Returns409()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 10, CreatedAt = DateTimeOffset.UtcNow };
        _productRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeductStockAsync(1, 2)).ReturnsAsync(false);

        var request = new PlaceOrderRequest(1, 2, "customer@example.com");
        var result = await _sut.PlaceOrderAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValid_CreatesOrderAndReturns201()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 10, CreatedAt = DateTimeOffset.UtcNow };
        var order = new Order
        {
            Id            = 1,
            ProductId     = 1,
            Product       = product,
            Quantity      = 2,
            CustomerEmail = "customer@example.com",
            Status        = OrderStatus.Pending,
            OrderDate     = DateTimeOffset.UtcNow,
            TotalPrice    = 1998m
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeductStockAsync(1, 2)).ReturnsAsync(true);
        _orderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(order);

        var request = new PlaceOrderRequest(1, 2, "customer@example.com");
        var result = await _sut.PlaceOrderAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(1998m, result.Value!.TotalPrice);
        Assert.Equal(OrderStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task PlaceOrderAsync_TotalPrice_IsCalculatedCorrectly()
    {
        var product = new Product { Id = 1, Name = "Monitor", Category = "Electronics", Price = 399.50m, Stock = 10, CreatedAt = DateTimeOffset.UtcNow };
        var order = new Order
        {
            Id = 1, ProductId = 1, Product = product,
            Quantity = 3, CustomerEmail = "test@test.com",
            Status = OrderStatus.Pending, OrderDate = DateTimeOffset.UtcNow,
            TotalPrice = 399.50m * 3
        };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeductStockAsync(1, 3)).ReturnsAsync(true);
        _orderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(order);

        var request = new PlaceOrderRequest(1, 3, "test@test.com");
        var result = await _sut.PlaceOrderAsync(request);

        Assert.Equal(1198.50m, result.Value!.TotalPrice);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderNotFound_Returns404()
    {
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Order?)null);

        var result = await _sut.CancelOrderAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenAlreadyCancelled_Returns400()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow };
        var order = new Order { Id = 1, ProductId = 1, Product = product, Quantity = 1, CustomerEmail = "x@x.com", Status = OrderStatus.Cancelled, OrderDate = DateTimeOffset.UtcNow, TotalPrice = 999m };

        _orderRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("already cancelled", result.Error);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenShipped_Returns400()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow };
        var order = new Order { Id = 1, ProductId = 1, Product = product, Quantity = 1, CustomerEmail = "x@x.com", Status = OrderStatus.Shipped, OrderDate = DateTimeOffset.UtcNow, TotalPrice = 999m };

        _orderRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("already shipped", result.Error);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenPending_CancelsAndRestoresStock()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow };
        var order = new Order { Id = 1, ProductId = 1, Product = product, Quantity = 2, CustomerEmail = "x@x.com", Status = OrderStatus.Pending, OrderDate = DateTimeOffset.UtcNow, TotalPrice = 1998m };
        var cancelled = new Order { Id = 1, ProductId = 1, Product = product, Quantity = 2, CustomerEmail = "x@x.com", Status = OrderStatus.Cancelled, OrderDate = order.OrderDate, TotalPrice = 1998m };

        _orderRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _productRepositoryMock.Setup(r => r.RestoreStockAsync(1, 2)).ReturnsAsync(true);
        _orderRepositoryMock.Setup(r => r.UpdateStatusAsync(1, OrderStatus.Cancelled)).ReturnsAsync(cancelled);

        var result = await _sut.CancelOrderAsync(1);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Cancelled, result.Value!.Status);
        _productRepositoryMock.Verify(r => r.RestoreStockAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_Returns404()
    {
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Order?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }
}
