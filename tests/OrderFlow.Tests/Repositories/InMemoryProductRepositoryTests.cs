using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Repositories;
using Xunit;

namespace OrderFlow.Tests.Repositories;

public class InMemoryProductRepositoryTests
{
    private readonly InMemoryProductRepository _sut = new();

    [Fact]
    public async Task GetAllAsync_ReturnsSeedData()
    {
        var products = await _sut.GetAllAsync();

        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProduct()
    {
        var product = await _sut.GetByIdAsync(1);

        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var product = await _sut.GetByIdAsync(999);

        Assert.Null(product);
    }

    [Fact]
    public async Task AddAsync_AssignsIdAndPersistsProduct()
    {
        var newProduct = new Product
        {
            Name      = "Webcam",
            Category  = "Accessories",
            Price     = 79.99m,
            Stock     = 15,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var added = await _sut.AddAsync(newProduct);

        Assert.True(added.Id > 0);

        var retrieved = await _sut.GetByIdAsync(added.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Webcam", retrieved.Name);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesProduct()
    {
        var deleted = await _sut.DeleteAsync(1);
        var retrieved = await _sut.GetByIdAsync(1);

        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeductStockAsync_WhenSufficientStock_DeductsAndReturnsTrue()
    {
        var before = await _sut.GetByIdAsync(1);
        var stockBefore = before!.Stock;

        var result = await _sut.DeductStockAsync(1, 2);

        var after = await _sut.GetByIdAsync(1);

        Assert.True(result);
        Assert.Equal(stockBefore - 2, after!.Stock);
    }

    [Fact]
    public async Task DeductStockAsync_WhenInsufficientStock_ReturnsFalse()
    {
        var result = await _sut.DeductStockAsync(1, 9999);

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreStockAsync_IncreasesStock()
    {
        var before = await _sut.GetByIdAsync(1);
        var stockBefore = before!.Stock;

        await _sut.RestoreStockAsync(1, 3);

        var after = await _sut.GetByIdAsync(1);
        Assert.Equal(stockBefore + 3, after!.Stock);
    }

    [Fact]
    public async Task SearchAsync_MatchesByName_CaseInsensitive()
    {
        var results = await _sut.SearchAsync("LAPTOP");

        Assert.NotEmpty(results);
        Assert.All(results, p => Assert.Contains("laptop", p.Name.ToLower()));
    }

    [Fact]
    public async Task SearchAsync_MatchesByCategory()
    {
        var results = await _sut.SearchAsync("Electronics");

        Assert.NotEmpty(results);
        Assert.All(results, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public async Task SearchAsync_WithNoMatch_ReturnsEmptyList()
    {
        var results = await _sut.SearchAsync("zzznonexistent");

        Assert.Empty(results);
    }
}
