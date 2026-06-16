using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderFlow.Application.DTOs;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;
using Xunit;

namespace OrderFlow.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _sut = new ProductService(_repositoryMock.Object, NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop",  Category = "Electronics", Price = 999m, Stock = 5,  CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = 2, Name = "Monitor", Category = "Electronics", Price = 399m, Stock = 10, CreatedAt = DateTimeOffset.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var result = await _sut.GetAllAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(1);

        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Laptop", result.Value!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_Returns404()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("99", result.Error);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedProduct()
    {
        var request = new CreateProductRequest("Keyboard", "Accessories", 129m, 20);
        var created = new Product { Id = 5, Name = "Keyboard", Category = "Accessories", Price = 129m, Stock = 20, CreatedAt = DateTimeOffset.UtcNow };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>())).ReturnsAsync(created);

        var result = await _sut.CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Keyboard", result.Value!.Name);
        Assert.Equal(129m, result.Value.Price);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_Returns404()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var request = new UpdateProductRequest("New Name", "Electronics", 500m, 3);
        var result = await _sut.UpdateAsync(99, request);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_UpdatesAndReturnsProduct()
    {
        var existing = new Product { Id = 1, Name = "Old Name", Category = "Electronics", Price = 100m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow };
        var updated  = new Product { Id = 1, Name = "New Name", Category = "Electronics", Price = 200m, Stock = 3, CreatedAt = existing.CreatedAt, UpdatedAt = DateTimeOffset.UtcNow };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(updated);

        var request = new UpdateProductRequest("New Name", "Electronics", 200m, 3);
        var result = await _sut.UpdateAsync(1, request);

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal(200m, result.Value.Price);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_Returns404()
    {
        _repositoryMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync(false);

        var result = await _sut.DeleteAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_Returns400()
    {
        var result = await _sut.SearchAsync(string.Empty);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMatchingProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 5, CreatedAt = DateTimeOffset.UtcNow }
        };

        _repositoryMock.Setup(r => r.SearchAsync("laptop")).ReturnsAsync(products);

        var result = await _sut.SearchAsync("laptop");

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
    }
}
