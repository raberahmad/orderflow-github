using Microsoft.Extensions.Logging;
using OrderFlow.Application.DTOs;
using OrderFlow.Domain.Interfaces;
using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync()
    {
        var products = await _repository.GetAllAsync();
        var response = products.Select(MapToResponse).ToList();
        return ServiceResult<IReadOnlyList<ProductResponse>>.Ok(response);
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.NotFound($"Product {id} not found.");
        }

        return ServiceResult<ProductResponse>.Ok(MapToResponse(product));
    }

    public async Task<ServiceResult<IReadOnlyList<ProductResponse>>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ServiceResult<IReadOnlyList<ProductResponse>>.BadRequest("Search query cannot be empty.");
        }

        var results = await _repository.SearchAsync(query);
        return ServiceResult<IReadOnlyList<ProductResponse>>.Ok(results.Select(MapToResponse).ToList());
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name      = request.Name,
            Category  = request.Category,
            Price     = request.Price,
            Stock     = request.Stock,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _repository.AddAsync(product);
        _logger.LogInformation("Product created with id {ProductId}", created.Id);
        return ServiceResult<ProductResponse>.Created(MapToResponse(created));
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            return ServiceResult<ProductResponse>.NotFound($"Product {id} not found.");
        }

        existing.Name      = request.Name;
        existing.Category  = request.Category;
        existing.Price     = request.Price;
        existing.Stock     = request.Stock;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        return ServiceResult<ProductResponse>.Ok(MapToResponse(updated!));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return ServiceResult<bool>.NotFound($"Product {id} not found.");
        }

        _logger.LogInformation("Product {ProductId} deleted", id);
        return ServiceResult<bool>.Ok(true);
    }

    private static ProductResponse MapToResponse(Product p)
    {
        return new ProductResponse(p.Id, p.Name, p.Category, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);
    }
}
