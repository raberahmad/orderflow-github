using System.ComponentModel.DataAnnotations;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.DTOs;

public record OrderResponse(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    string CustomerEmail,
    OrderStatus Status,
    DateTimeOffset OrderDate,
    decimal TotalPrice
);

public record PlaceOrderRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, 1_000)]        int Quantity,
    [Required, EmailAddress] string CustomerEmail
);
