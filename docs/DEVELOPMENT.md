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
4. Start the forward Blazor host:

```bash
dotnet run --project src/CoroMES.Web
```

Start the backend API directly when working on route behavior:

```bash
dotnet run --project src/CoroMES.Api
```

The Codespace starts PostgreSQL and MQTT automatically. The API uses PostgreSQL with this test connection:

```text
Host=postgres;Port=5432;Database=coromes;Username=postgres;Password=changeme
```

Forwarded ports are private by default:

- `5000` - CoroMES API or forwarded development host
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

### 5. Run the Blazor Host

```powershell
dotnet run --project src/CoroMES.Web
```

`src/CoroMES.Web` is the forward UI host for admin and shop-floor display workflows. `src/CoroMES.Api` remains the backend API host and route contract source of truth.

The Blazor host route surface is split into focused files under `src/CoroMES.Web/Endpoints`. Keep new host endpoints in those modules, and add application services only when the endpoint logic grows beyond straightforward mapping.

### 6. Sign In To Admin

The Blazor host protects `/admin`, `/displays/builder`, and `/api/v1/*` with the first cookie-based admin gate.

For local development, use the fallback access code:

```text
dev-admin
```

For shared or production-like environments, set an access code through configuration instead of committing one:

```powershell
$env:Auth__AdminAccessCode = "<set-a-real-local-secret>"
dotnet run --project src/CoroMES.Web
```

Shop-floor viewer routes such as `/displays/viewer?id=preview&type=oee` remain anonymous during migration. Saved display definitions can also be opened by slug, for example `/displays/viewer?id=line1-oee`.

### 7. Access Swagger

Visit: http://localhost:5000/swagger

## Project Structure

```
src/
├── CoroMES.Web/           # Forward Blazor host
├── CoroMES.Api/           # Backend minimal API
├── CoroMES.Application/  # Use cases, services
├── CoroMES.Core/         # Entities, interfaces
├── CoroMES.Infrastructure/# DB, external clients
└── CoroMES.Reporting/    # Analytics endpoints
```

## Adding a New Feature

For UI work, add Blazor pages/components in `src/CoroMES.Web` and preserve the existing backend route contract unless an API migration is explicitly planned. Legacy static URLs such as `/admin`, `/displays/viewer.html`, and `/displays/builder.html` should redirect to the matching Blazor route or remain compatible until the replacement is complete.

For API parity work in the Blazor host:

- Add route mappings under `src/CoroMES.Web/Endpoints`.
- Keep startup-only database checks in `src/CoroMES.Web/Startup`.
- Add persisted data to `ApplicationDbContext` and EF migrations.
- Update `docs/API.md`, `docs/BLAZOR_MIGRATION.md`, and memory files in the same change.
- Keep `dotnet test CoroMES.sln` green before handing the app back.

### UpKeep Integration Modes

The Blazor host registers `CoroMES.Integration.Upkeep` through `AddUpkeepIntegration`.

```powershell
$env:Upkeep__Mode = "mock"      # default local mode
$env:Upkeep__Mode = "disabled"  # no assets, reject sync/downtime
$env:Upkeep__Mode = "live"      # requires BaseUrl and ApiKey
$env:Upkeep__BaseUrl = "https://api.onupkeep.com/"
$env:Upkeep__ApiKey = "<set-a-real-local-secret>"
```

Do not enter UpKeep credentials in the Blazor settings scaffold. Use environment variables, user secrets, or deployment configuration.

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

## Blazor Migration Notes

- Forward host: `src/CoroMES.Web`
- Backend API host: `src/CoroMES.Api`
- Preservation branch/tag: `pre-blazor-2026-06-10`
- Source backup zip: `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`
- Route parity reference: `docs/API.md`
- Migration detail: `docs/BLAZOR_MIGRATION.md`

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
