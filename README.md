# AnyWare Software — Task Management API

A backend Task Management API built with **.NET 10 / ASP.NET Core**, following a
**Domain-Driven, layered (Clean) architecture**. It supports user registration and
authentication, role-based administration, and full task management with caching and
background processing.

> Built as a technical assessment. The focus is clean structure, correct use of common
> backend building blocks (auth, caching, background work, migrations), and readable code.

---

## Table of contents
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Features](#features)
- [Getting started](#getting-started)
- [Database & seeding](#database--seeding)
- [Authentication & authorization](#authentication--authorization)
- [API reference](#api-reference)
- [Testing](#testing)
- [Design decisions & assumptions](#design-decisions--assumptions)
- [Possible improvements](#possible-improvements)

---

## Tech stack

| Concern | Technology |
| --- | --- |
| Framework | .NET 10 / ASP.NET Core Web API |
| Data access | Entity Framework Core 10 (SQL Server) |
| Identity & auth | ASP.NET Core Identity + JWT (access + refresh tokens) |
| Caching | Redis (StackExchange.Redis) |
| Background work | `BackgroundService` + in-memory `Channel` queue |
| API docs | Swagger / OpenAPI (with JWT support) |
| Testing | xUnit, Moq, EF Core InMemory |

---

## Architecture

The solution is split into four projects with a strict, one-directional dependency flow
(**API → Application → Domain**, and **Infrastructure → Application/Domain**). The Domain
has no dependencies on the outer layers, which keeps the business rules isolated and
testable.

```
          ┌─────────────┐
          │     API     │  Controllers, middleware, JWT & Swagger setup
          └──────┬──────┘
                 │
          ┌──────▼──────┐
          │ Application │  Services, DTOs, interfaces, business rules
          └──────┬──────┘
                 │
          ┌──────▼──────┐
          │   Domain    │  Entities, enums, repository/Unit Of Work abstractions
          └─────────────┘
                 ▲
          ┌──────┴──────┐
          │Infrastructure│  EF Core DbContext, repositories, Redis, workers
          └─────────────┘
```

| Layer | Responsibility |
| --- | --- |
| **Domain** | Entities (`ApplicationUser`, `TaskItem`, `RefreshToken`, `BaseEntity`), enums, and the `IBaseRepository<T>` / `IUnitOfWork` abstractions. No external dependencies. |
| **Application** | Business logic (`UserService`, `TaskService`), DTOs, service interfaces, and typed exceptions. Depends only on Domain. |
| **Infrastructure** | EF Core `DbContext` (seeding, soft delete, audit dates), generic repository + unit of work, Redis cache, background queue & worker, and DI registration. |
| **API** | Controllers, JWT authentication, Swagger, and global exception-handling middleware. |

---

## Project structure

```
AnyWareSoftWare/
├── AnyWareSoftWare.Domain/
│   ├── Entities/         ApplicationUser, TaskItem, RefreshToken, BaseEntity
│   ├── Enums/            TaskStatus, TaskPriority
│   └── Interfaces/       IBaseRepository<T>, IUnitOfWork
├── AnyWareSoftWare.Application/
│   ├── DTOs/             Auth & task DTOs
│   ├── Exceptions/       AppException (+ NotFound/Conflict/Unauthorized)
│   ├── Interfaces/       IUserService, ITaskService, cache & queue contracts
│   └── Services/         UserService, TaskService
├── AnyWareSoftWare.Infrastructure/
│   ├── Data/             AppDbContext (+ seeding, soft delete, audit)
│   ├── Repositories/     BaseRepository<T>
│   ├── UnitOfWork/       UnitOfWork
│   ├── Services/         RedisCacheService, BackgroundQueue, TaskProcessingWorker
│   └── Migrations/
├── AnyWareSoftWare.API/
│   ├── Controllers/      UsersController, TasksController
│   ├── Middleware/       ExceptionHandlingMiddleware
│   └── Program.cs
└── AnyWareSoftWare.Tests/    xUnit tests
```

---

## Features

- **Users** — registration, login, and current-profile endpoint.
- **Admin** — a seeded admin who can create, list, and (soft) delete users; these
  actions are restricted to the `Admin` role.
- **Tasks** — create, get by id, list, and update status; every task belongs to a user,
  and users can only see and modify their own tasks.
- **JWT authentication** with **refresh tokens** (rotated on use — the old token is
  revoked when a new pair is issued).
- **Redis caching** on *Get Task by ID*, invalidated when the task is updated. Caching is
  best-effort: if Redis is down, reads fall back to the database.
- **Background processing** — creating a task enqueues it to a `BackgroundService` that
  simulates processing and updates the task.
- **Business rules** — tasks are returned **sorted by priority, then creation date**, and
  a user cannot create two tasks with the **same title on the same day**.
- **Soft delete** for users, plus automatic `CreatedAt` / `UpdatedAt` auditing, handled
  centrally in the `DbContext.SaveChanges` override.
- **Global exception handling** — business errors map to proper HTTP status codes
  (`400 / 401 / 404 / 409`) with a JSON body, instead of leaking `500`s.

---

## Getting started

### Prerequisites
- **.NET 10 SDK**
- **SQL Server** — LocalDB (ships with Visual Studio) or any SQL Server instance
- *(Optional)* **Redis** on `localhost:6379` — the app runs without it; caching simply
  falls back to the database.

### 1. Clone
```bash
git clone https://github.com/3laamobarak/Anyware-Software.git
cd Anyware-Software
```

### 2. Configure
Edit `AnyWareSoftWare.API/appsettings.json` if needed.

Default database (LocalDB):
```
Server=(localdb)\MSSQLLocalDB; Database=AnyWareSoftWareDb; Trusted_Connection=True; Encrypt=False; MultipleActiveResultSets=True;
```
To target a remote SQL Server, replace `DefaultConnection` with your own, e.g.:
```
Server=<host>; Database=<db>; User Id=<user>; Password=<password>; Encrypt=False; MultipleActiveResultSets=True;
```

### 3. Run
```bash
dotnet run --project AnyWareSoftWare.API
```
On first run the app **applies migrations and seeds** the roles + admin automatically,
then opens Swagger at:

```
http://localhost:5172/swagger
```

---

## Database & seeding

- The schema is managed with **EF Core migrations**, applied automatically on startup.
  To apply them manually instead:
  ```bash
  dotnet ef database update --project AnyWareSoftWare.Infrastructure --startup-project AnyWareSoftWare.API
  ```
- **Seed data** (the `Admin`/`User` roles and the default admin) is defined with
  `HasData` inside `AppDbContext`, so it is part of the migration and exists as soon as
  the database is created.

### Seeded admin credentials
| Field | Value |
| --- | --- |
| Email | `admin@example.com` |
| Password | `Admin@123` |
| Role | `Admin` |

---

## Authentication & authorization

1. **Log in** (`POST /api/users/login`) → receive an **access token** (JWT) and a
   **refresh token**.
2. Send the access token as `Authorization: Bearer <token>` on protected endpoints.
3. When the access token expires, call **`POST /api/users/refresh-token`** with the
   refresh token to get a new pair. The used refresh token is revoked (rotation).

Roles are embedded as claims in the JWT; admin-only endpoints are guarded with
`[Authorize(Roles = "Admin")]`.

**Using Swagger:** log in, copy the `accessToken`, click **Authorize** (top-right), paste
the token (no `Bearer ` prefix), and call the protected endpoints.

---

## API reference

Base URL: `http://localhost:5172`

### Users & auth
| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/users/register` | Anonymous | Register a new user |
| POST | `/api/users/login` | Anonymous | Log in → access + refresh token |
| POST | `/api/users/refresh-token` | Anonymous | Exchange a refresh token for a new pair |
| GET | `/api/users/me` | Authenticated | Current user's profile |
| GET | `/api/users/admin/users` | Admin | List all users |
| POST | `/api/users/admin/users` | Admin | Create a user |
| DELETE | `/api/users/admin/users/{id}` | Admin | Soft-delete a user |

### Tasks
| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/tasks` | Authenticated | Create a task (queued for background processing) |
| GET | `/api/tasks` | Authenticated | List the caller's tasks (priority, then date) |
| GET | `/api/tasks/{id}` | Authenticated | Get one of the caller's tasks (Redis-cached) |
| PUT | `/api/tasks/{id}/status` | Authenticated | Update task status (invalidates cache) |

### Example
```bash
# 1) Log in
curl -X POST http://localhost:5172/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@123"}'

# 2) Create a task (use the accessToken from step 1)
curl -X POST http://localhost:5172/api/tasks \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"title":"Write report","description":"Q3 summary","priority":2}'
```

`priority`: `0 = Low`, `1 = Medium`, `2 = High`. Task `status`: `Pending`,
`InProgress`, `Done`.

---

## Testing

Unit tests use **xUnit**, with **Moq** for dependencies and **EF Core InMemory** for the
database.

```bash
dotnet test
```

Coverage includes: task creation + background enqueue, duplicate-title prevention,
per-user title uniqueness, priority-then-date sorting, ownership isolation, status update
+ cache invalidation, and soft-delete behavior.

---

## Design decisions & assumptions

- **Integer keys for Identity** (`IdentityUser<int>` / `IdentityRole<int>`), so a task's
  `UserId` is a simple `int`.
- **Two roles** — `Admin` and `User`. New registrations receive `User`; the seeded
  account is the only `Admin`.
- **JWT** — issuer/audience validation is disabled for simplicity (only the signing key
  is validated). Access token lifetime is configurable (`Jwt:ExpiresInMinutes`, default
  60 minutes); refresh tokens live 7 days and are rotated on use.
- **Soft delete applies to users.** Deleting a user sets `IsDeleted = true` in the
  `SaveChanges` override (Identity's `UserManager` deletes bypass the repository, so the
  DbContext is the reliable interception point). A global query filter hides soft-deleted
  rows.
- **Duplicate-title rule** is evaluated against the current **UTC** date.
- **Redis is optional** — cache operations are best-effort and never fail a request.
- **Migrations run on startup** for convenience during review; in production this would
  typically be a separate deployment step.
- **Secrets in `appsettings.json`** (JWT key) are committed for ease of review. In a real
  project these belong in User Secrets / environment variables and would be rotated.

---

## Possible improvements

- Move secrets to User Secrets / environment variables and rotate the JWT key.
- Add FluentValidation for request DTOs and standardize error responses (ProblemDetails).
- Add integration tests (e.g. `WebApplicationFactory`) alongside the unit tests.
- Add pagination and filtering to the task and user list endpoints.
- Add structured logging and health checks.
