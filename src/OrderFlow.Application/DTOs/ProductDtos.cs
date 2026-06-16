using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.DTOs;

public record ProductResponse(
    int Id,
    string Name,
    string Category,
    decimal Price,
    int Stock,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record CreateProductRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(2), MaxLength(50)]  string Category,
    [Range(0.01, 1_000_000)]                 decimal Price,
    [Range(0, 100_000)]                      int Stock
);

public record UpdateProductRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(2), MaxLength(50)]  string Category,
    [Range(0.01, 1_000_000)]                 decimal Price,
    [Range(0, 100_000)]                      int Stock
);
