using OrderFlow.Domain.Models;

namespace OrderFlow.Domain.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<IReadOnlyList<Product>> SearchAsync(string query);
    Task<Product> AddAsync(Product product);
    Task<Product?> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeductStockAsync(int productId, int quantity);
    Task<bool> RestoreStockAsync(int productId, int quantity);
}
