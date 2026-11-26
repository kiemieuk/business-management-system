# Business Management System (BMS)

BMS is a modular business management platform. It follows a **service-oriented architecture**: each business module is its own **.NET microservice** with clean layers (Domain → Application → Persistence → API). All services sit behind an **API gateway**, and a **React** single-page app is the frontend.

The first module is **Identity**. It provides:
- username/password login
- passwords hashed with PBKDF2-SHA512 and a per-user salt (never stored in plain text)
- JWT access tokens
- users, roles and fine-grained permissions, all manageable from the UI

## Architecture

```
            ┌──────────────────────┐
            │  React web app (SPA) │  src/Web/bms-web
            └──────────┬───────────┘
                       │  /api/*
            ┌──────────▼───────────┐
            │  API Gateway (YARP)  │  src/ApiGateway/BMS.ApiGateway
            └──────────┬───────────┘
       /api/identity/* │      /api/<module>/*  (future)
            ┌──────────▼───────────┐   ┌─────────────────────┐
            │  Identity service    │   │  Next module …      │
            │  (own database)      │   │  (own database)     │
            └──────────────────────┘   └─────────────────────┘
```

| Folder | Purpose |
| --- | --- |
| `src/BuildingBlocks/BMS.BuildingBlocks.Domain` | Base `Entity`, `AuditableEntity`, `IAggregateRoot`, `IRepository<T>`, `IUnitOfWork`, `ICurrentUser`, domain exceptions |
| `src/BuildingBlocks/BMS.BuildingBlocks.Contracts` | Contracts shared between services, including the central **permission catalogue** (`Permissions`) |
| `src/BuildingBlocks/BMS.BuildingBlocks.Persistence` | Entity Framework Core base `DbContext`, generic repository, audit interceptor, database provider selection (SQL Server / SQLite) |
| `src/BuildingBlocks/BMS.BuildingBlocks.Web` | `AddServiceDefaults()` / `UseServiceDefaults()`: JWT auth, permission-based authorization (`[HasPermission]`), ProblemDetails error handling, OpenAPI + Scalar, health checks |
| `src/Services/Identity/BMS.Identity.Domain` | **Domain layer**: `User`, `Role`, `UserRole`, `RolePermission` aggregates and repository interfaces |
| `src/Services/Identity/BMS.Identity.Application` | Use cases (`AuthService`, `UserService`, `RoleService`), DTOs, password hashing, token issuing |
| `src/Services/Identity/BMS.Identity.Persistence` | **Persistence layer**: EF Core `IdentityDbContext`, entity configurations, repositories, migrations, data seeding |
| `src/Services/Identity/BMS.Identity.API` | REST API (controllers) and the service host |
| `src/ApiGateway/BMS.ApiGateway` | YARP reverse proxy: the single entry point for the frontend |
| `src/Web/bms-web` | **Frontend layer**: React + TypeScript (Vite) |
| `tests/BMS.Identity.Tests` | xUnit unit and integration tests (`WebApplicationFactory`) |

Each microservice owns its database (the Identity tables live in the `identity` schema). Services never share tables. They talk to each other only through APIs and contracts.

### Security model

- **Passwords**: PBKDF2 with HMAC-SHA512, 210,000 iterations, 128-bit random salt, constant-time comparison. The hash string stores the algorithm and iteration count so the parameters can be raised later. Password policy: 8–128 characters with upper case, lower case and a digit.
- **Brute-force protection**: an account locks after `AccountSecurity:MaxFailedAccessAttempts` failed logins (default 5) for `LockoutMinutes` (default 15). The failed-attempt counter is updated atomically in the database, so parallel guesses cannot bypass it. Login takes the same time whether or not the user exists.
- **Role-based access**: users have roles, and roles have permissions (e.g. `identity.users.create`). Endpoints are protected with `[HasPermission(Permissions.Identity.Users.Create)]`. The JWT contains the user's roles and permissions, and the frontend uses them to show or hide menu items and buttons.
- **System roles**:
  - `Administrator` always holds every permission and cannot be deleted or renamed. The last active administrator cannot be removed or deactivated.
  - `User` is the default role for new users.
- **No privilege escalation**: a user who is not an administrator can only grant permissions they hold themselves. The same applies to roles they assign or edit, and to users they manage (edit, reset password, change roles, delete). Only administrators can grant the Administrator role or manage administrator accounts.
- Role and permission changes take effect the next time the user signs in.

## Getting started (local development)

Prerequisites: [.NET SDK 10](https://dotnet.microsoft.com/download) and Node.js 20+.

In Development the Identity service uses a local **SQLite** file, so no database server is needed.

```bash
# 1. Identity service (http://localhost:5101, API docs at /scalar)
dotnet run --project src/Services/Identity/BMS.Identity.API

# 2. API gateway (http://localhost:5000)
dotnet run --project src/ApiGateway/BMS.ApiGateway

# 3. Web app (http://localhost:5173; proxies /api to the gateway)
cd src/Web/bms-web
npm install
npm run dev
```

Sign in with the development administrator **`admin` / `Admin@12345`**, which is configured in `appsettings.Development.json`. Change the password after the first login.

### Run everything with Docker (SQL Server)

```bash
cp .env.example .env     # then edit the secrets
docker compose up --build
```

Then open http://localhost:8080. The gateway is also exposed on http://localhost:5000.

## Configuration

Settings use standard ASP.NET Core configuration. Provide secrets through environment variables or a secret store; never commit them.

| Setting | Env variable | Description |
| --- | --- | --- |
| `Database:Provider` | `Database__Provider` | `SqlServer` (default) or `Sqlite` |
| `Database:ConnectionString` | `Database__ConnectionString` | Connection string for the service database |
| `Jwt:SigningKey` | `Jwt__SigningKey` | HMAC key, **at least 32 bytes**. Must be the same for every service that validates tokens |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:AccessTokenLifetimeMinutes` | | Token settings |
| `Seed:AdminPassword` | `Seed__AdminPassword` | Creates the initial `admin` user when the database has no users. Leave empty to skip |
| `AccountSecurity:*` | | Lockout settings |

On SQL Server the Identity service applies EF Core migrations automatically at startup. On SQLite it creates the schema directly.

## Build and test

```bash
dotnet build BusinessManagementSystem.sln
dotnet test BusinessManagementSystem.sln

cd src/Web/bms-web && npm run build && npm run lint
```

### EF Core migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  -p src/Services/Identity/BMS.Identity.Persistence \
  -s src/Services/Identity/BMS.Identity.API -o Migrations
```

## Adding a new module (e.g. Inventory)

1. **Create the projects** under `src/Services/Inventory`, following the Identity layout:
   - `BMS.Inventory.Domain` references `BuildingBlocks.Domain`
   - `BMS.Inventory.Application` references the Domain project
   - `BMS.Inventory.Persistence` references `BuildingBlocks.Persistence`
   - `BMS.Inventory.API` references `BuildingBlocks.Web`

   Add them to the solution with `dotnet sln add`.
2. **Domain**: model entities by deriving from `AuditableEntity`, and mark aggregates with `IAggregateRoot`.
3. **Persistence**:
   - Create `InventoryDbContext : BaseDbContext` with its own schema.
   - Register it with `services.AddServiceDbContext<InventoryDbContext>(configuration)`.
   - Use `EfRepository<,>` for repositories.
4. **Permissions**: add a nested class to `BuildingBlocks.Contracts/Authorization/Permissions.cs` (for example `Permissions.Inventory.Products.View`) and include it in `All`.
   - The Administrator role picks up new permissions automatically at startup.
   - The new permissions appear in the role editor.
5. **API**: in `Program.cs`, call `builder.Services.AddServiceDefaults(builder.Configuration)` and `app.UseServiceDefaults()`, then protect endpoints with `[HasPermission(...)]`.
6. **Gateway**: add a route (`/api/inventory/{**catch-all}`) and a cluster in `src/ApiGateway/BMS.ApiGateway/appsettings.json`.
7. **Frontend**: add pages under `src/Web/bms-web/src/features/inventory`, register routes in `App.tsx`, and add menu entries (with the required permission) in `src/layout/navigation.ts`.
8. **Docker**: add a Dockerfile and a service entry in `docker-compose.yml`.

## Suggested roadmap

- Refresh tokens and token revocation, so permission changes apply immediately
- Rate limiting on the login endpoint
- Integration events over a message bus (e.g. RabbitMQ or Azure Service Bus with MassTransit) for communication between services
- Multi-tenancy and per-company data separation
- Audit trail service, centralized logging and tracing with OpenTelemetry, and .NET Aspire for local orchestration
- CI/CD pipeline and Kubernetes deployment
- Business modules: HR, Inventory, Sales/CRM, Purchasing, Finance/Accounting, Reporting
