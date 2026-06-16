using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;

namespace OrderFlow.Infrastructure.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products =
    [
        new() { Id = 1, Name = "Laptop Pro 16",      Category = "Electronics", Price = 1299.99m, Stock = 10, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = 2, Name = "Ergonomic Chair",     Category = "Furniture",   Price =  349.00m, Stock = 5,  CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = 3, Name = "4K Monitor 27\"",     Category = "Electronics", Price =  499.50m, Stock = 8,  CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = 4, Name = "Mechanical Keyboard", Category = "Accessories", Price =  129.00m, Stock = 20, CreatedAt = DateTimeOffset.UtcNow },
    ];

    private int _nextId = 5;

    public Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Product>>(_products.AsReadOnly());
    }

    public Task<Product?> GetByIdAsync(int id)
    {
        return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
    }

    public Task<IReadOnlyList<Product>> SearchAsync(string query)
    {
        var lower = query.ToLowerInvariant();

        var results = _products
            .Where(p => p.Name.Contains(lower, StringComparison.OrdinalIgnoreCase)
                     || p.Category.Contains(lower, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<Product>>(results);
    }

    public Task<Product> AddAsync(Product product)
    {
        product.Id = _nextId++;
        _products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(Product product)
    {
        var index = _products.FindIndex(p => p.Id == product.Id);

        if (index == -1)
        {
            return Task.FromResult<Product?>(null);
        }

        _products[index] = product;
        return Task.FromResult<Product?>(product);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);

        if (product is null)
        {
            return Task.FromResult(false);
        }

        _products.Remove(product);
        return Task.FromResult(true);
    }

    public Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);

        if (product is null || product.Stock < quantity)
        {
            return Task.FromResult(false);
        }

        product.Stock -= quantity;
        return Task.FromResult(true);
    }

    public Task<bool> RestoreStockAsync(int productId, int quantity)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);

        if (product is null)
        {
            return Task.FromResult(false);
        }

        product.Stock += quantity;
        return Task.FromResult(true);
    }
}
