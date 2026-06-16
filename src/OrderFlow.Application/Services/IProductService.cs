using OrderFlow.Application.DTOs;

namespace OrderFlow.Application.Services;

public interface IProductService
{
    Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync();
    Task<ServiceResult<ProductResponse>> GetByIdAsync(int id);
    Task<ServiceResult<IReadOnlyList<ProductResponse>>> SearchAsync(string query);
    Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request);
    Task<ServiceResult<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int id);
}
