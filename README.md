# OrderFlow

ASP.NET Core 10 order management backend built with Clean Architecture, used to demonstrate AI-powered PR review via Azure OpenAI + Azure DevOps Pipelines.

## Solution structure

```
OrderFlow/
├── src/
│   ├── OrderFlow.Domain          # Models, enums, repository interfaces
│   ├── OrderFlow.Application     # DTOs, service interfaces, business logic
│   ├── OrderFlow.Infrastructure  # In-memory repository implementations
│   └── OrderFlow.Api             # Controllers, Program.cs
└── tests/
    └── OrderFlow.Tests           # xUnit tests (services + repositories)
```

## Running locally

```bash
cd src/OrderFlow.Api
dotnet run
# Swagger UI at http://localhost:5000
```

## Running tests

```bash
dotnet test
```

## API endpoints

| Method | Path                        | Description       |
|--------|-----------------------------|-------------------|
| GET    | /api/products               | List products     |
| GET    | /api/products/{id}          | Get by id         |
| GET    | /api/products/search?query= | Search products   |
| POST   | /api/products               | Create product    |
| PUT    | /api/products/{id}          | Update product    |
| DELETE | /api/products/{id}          | Delete product    |
| GET    | /api/orders                 | List orders       |
| GET    | /api/orders/{id}            | Get by id         |
| POST   | /api/orders                 | Place order       |
| POST   | /api/orders/{id}/cancel     | Cancel order      |

## Intentional flaws for PR review demo

- `InMemoryProductRepository` and `InMemoryOrderRepository` use `List<T>` without locking — unsafe under concurrent requests
- `InMemoryProductRepository.SearchAsync` does not guard against a null `query` argument
- `OrderService.PlaceOrderAsync` calculates `TotalPrice` after the stock deduction — a price change in between produces an inconsistent total