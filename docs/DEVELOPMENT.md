# Development Guide

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code
- PostgreSQL (via Docker)

## Getting Started

### GitHub Codespaces

This repository includes a `.devcontainer` setup for free DEV/TEST use in GitHub Codespaces.

1. Push the branch to GitHub.
2. In GitHub, select **Code > Codespaces > Create codespace**.
3. Wait for package restore to finish.
4. Start the API:

```bash
dotnet run --project src/CoroMES.Api
```

The Codespace starts PostgreSQL and MQTT automatically. The API uses PostgreSQL with this test connection:

```text
Host=postgres;Port=5432;Database=coromes;Username=postgres;Password=changeme
```

Forwarded ports are private by default:

- `5000` - CoroMES API
- `5432` - PostgreSQL
- `1883` - MQTT
- `9001` - MQTT WebSockets

### 1. Clone and Setup

```powershell
git clone <repo-url> CoroMES
cd CoroMES
```

### 2. Start Infrastructure

```powershell
# Start PostgreSQL and MQTT
docker compose -f infra/docker/docker-compose.yml up -d
```

### 3. Build the Solution

```powershell
dotnet build
```

### 4. Run the API

```powershell
dotnet run --project src/CoroMES.Api
```

### 5. Access Swagger

Visit: http://localhost:5000/swagger

## Project Structure

```
src/
├── CoroMES.Api/           # Entry point, minimal API
├── CoroMES.Application/  # Use cases, services
├── CoroMES.Core/         # Entities, interfaces
├── CoroMES.Infrastructure/# DB, external clients
└── CoroMES.Reporting/    # Analytics endpoints
```

## Adding a New Feature

### 1. Create Entity (Core)

```csharp
// src/CoroMES.Core/Entities/WorkOrder.cs
namespace CoroMES.Core.Entities;

public class WorkOrder
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public WorkOrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum WorkOrderStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled
}
```

### 2. Create Interface (Core)

```csharp
// src/CoroMES.Core/Interfaces/IWorkOrderRepository.cs
namespace CoroMES.Core.Interfaces;

public interface IWorkOrderRepository
{
    Task<IEnumerable<WorkOrder>> GetAllAsync();
    Task<WorkOrder?> GetByIdAsync(int id);
    Task<WorkOrder> CreateAsync(WorkOrder workOrder);
    Task UpdateAsync(WorkOrder workOrder);
}
```

### 3. Create Repository Implementation (Infrastructure)

```csharp
// src/CoroMES.Infrastructure/Repositories/WorkOrderRepository.cs
namespace CoroMES.Infrastructure.Repositories;

public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public WorkOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<WorkOrder>> GetAllAsync()
        => await _context.WorkOrders.ToListAsync();
    
    // ... other methods
}
```

### 4. Create Use Case (Application)

```csharp
// src/CoroMES.Application/UseCases/GetAllWorkOrdersQuery.cs
namespace CoroMES.Application.UseCases;

public class GetAllWorkOrdersQueryHandler 
    : IRequestHandler<GetAllWorkOrdersQuery, IEnumerable<WorkOrderDto>>
{
    private readonly IWorkOrderRepository _repository;
    
    public GetAllWorkOrdersQueryHandler(IWorkOrderRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<IEnumerable<WorkOrderDto>> Handle(
        GetAllWorkOrdersQuery request, 
        CancellationToken cancellationToken)
    {
        var workOrders = await _repository.GetAllAsync();
        return workOrders.MapTo<WorkOrderDto>();
    }
}
```

### 5. Create API Endpoint (Api)

```csharp
// src/CoroMES.Api/Endpoints/WorkOrdersEndpoints.cs
namespace CoroMES.Api.Endpoints;

public static class WorkOrdersEndpoints
{
    public static void MapWorkOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workorders", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetAllWorkOrdersQuery();
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        });
    }
}
```

### 6. Register Services (Api/Program.cs)

```csharp
// src/CoroMES.Api/Program.cs
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(GetAllWorkOrdersQueryHandler).Assembly));
```

## Running Tests

```powershell
# Run all tests
dotnet test

# Run specific project
dotnet test tests/CoroMES.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Code Style

- Use **C# 12** features (primary constructors, collection expressions)
- Follow **Microsoft C# Coding Conventions**
- Add XML documentation for public APIs
- Use **PascalCase** for types, **camelCase** for locals

## Configuration

Edit `config/settings.yaml`:

```yaml
database:
  host: localhost
  port: 5432
  name: coromes
  user: postgres
  password: changeme

app:
  host: 0.0.0.0
  port: 5000
  environment: development
```

## Common Commands

```powershell
# Add a new package
dotnet add src/CoroMES.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL

# Add a new project reference
dotnet add src/CoroMES.Application reference src/CoroMES.Core

# Create migration
dotnet ef migrations add InitialCreate --project src/CoroMES.Infrastructure

# Run migration
dotnet ef database update --project src/CoroMES.Infrastructure

# Publish
dotnet publish src/CoroMES.Api -c Release -o ./publish
```

## Troubleshooting

### Database Connection Failed
- Ensure Docker is running: `docker ps`
- Check PostgreSQL container: `docker logs coromes-postgres`
- Verify port 5432 is not in use

### Build Errors
- Restore packages: `dotnet restore`
- Clean build: `dotnet clean`

### Swagger Not Working
- Ensure `app.UseSwagger()` is called in Program.cs
- Check `config/settings.yaml` has `features.enableSwagger: true`

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make changes and commit: `git commit -m "feat: add feature"`
3. Push and create PR: `git push origin feature/your-feature`

Use the repo memory files and the active docs index as the current contribution guide until a dedicated `CONTRIBUTING.md` is added.
