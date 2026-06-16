using OrderFlow.Application.DTOs;

namespace OrderFlow.Application.Services;

public interface IOrderService
{
    Task<ServiceResult<IReadOnlyList<OrderResponse>>> GetAllAsync();
    Task<ServiceResult<OrderResponse>> GetByIdAsync(int id);
    Task<ServiceResult<OrderResponse>> PlaceOrderAsync(PlaceOrderRequest request);
    Task<ServiceResult<OrderResponse>> CancelOrderAsync(int id);
}
