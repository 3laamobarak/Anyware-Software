# AnyWareSoftWare — Task Management Backend API

A Task Management backend built with **.NET 10 / ASP.NET Core Web API**, using a
DDD-style layered structure: **API**, **Application**, **Domain**, **Infrastructure**
(plus a **Tests** project).

## Features

- ASP.NET Core **Identity** (integer keys) for users & roles
- **JWT** authentication + **refresh tokens** (with rotation)
- Role-based authorization (`Admin` / `User`)
- EF Core (SQL Server / LocalDB) with **migrations**
- **Database seeding** of roles + a default admin (via `HasData` in the DbContext)
- **Redis** caching for *Get Task by ID* (degrades gracefully if Redis is down)
- **Background processing** of created tasks (`BackgroundService` + in-memory queue)
- **Swagger / OpenAPI** with JWT support
- Generic **repository + unit of work**
- **Soft delete** for users (handled in the DbContext `SaveChanges` override)
- Audit timestamps (`CreatedAt` / `UpdatedAt`) maintained automatically
- Business rules: tasks sorted by priority then creation date; duplicate task titles
  blocked per user per day
- Global exception handling middleware (business errors → proper 4xx responses)

---

## Setup instructions

### Prerequisites
- **.NET 10 SDK**
- **SQL Server** — LocalDB (installed with Visual Studio) or any SQL Server / remote instance
- *(Optional)* **Redis** on `localhost:6379` — the app runs fine without it; caching just
  falls back to the database.

### 1. Configure the database connection
Edit `ConnectionStrings:DefaultConnection` in
[`AnyWareSoftWare.API/appsettings.json`](AnyWareSoftWare.API/appsettings.json).

Default (LocalDB):
```
Server=(localdb)\MSSQLLocalDB; Database=AnyWareSoftWareDb; Trusted_Connection=True; Encrypt=False; MultipleActiveResultSets=True;
```
For a remote SQL Server, replace it with your server/credentials, e.g.:
```
Server=<host>; Database=<db>; User Id=<user>; Password=<password>; Encrypt=False; MultipleActiveResultSets=True;
```

### 2. (Optional) Configure Redis
`ConnectionStrings:Redis` defaults to `localhost`. If you don't run Redis, no action is
needed — the app keeps working without caching.

### 3. Restore packages
```bash
dotnet restore
```

> Migrations are applied **automatically on startup**, so you don't need to run
> `dotnet ef database update` manually. If you prefer to run it yourself:
> ```bash
> dotnet ef database update --project AnyWareSoftWare.Infrastructure --startup-project AnyWareSoftWare.API
> ```

---

## How to run the project

```bash
dotnet run --project AnyWareSoftWare.API
```

On first run the app creates the database (applies migrations) and seeds the roles and
admin user. Swagger opens automatically in the browser at:

```
http://localhost:5172/swagger
```
*(or `https://localhost:7216/swagger` for the https profile)*

> Running from **Visual Studio / Rider** (F5) also opens Swagger automatically.
> If you run from the terminal and want auto-reload + auto-open, use
> `dotnet watch run --project AnyWareSoftWare.API`.

### Testing the API in Swagger
1. `POST /api/users/login` with the seeded admin credentials → copy the `accessToken`.
2. Click **Authorize** (top right), paste the token (no `Bearer ` prefix), Authorize.
3. Call the protected endpoints (tasks, admin user management, etc.).
4. When the access token expires, call `POST /api/users/refresh` with your `refreshToken`
   to get a new pair.

### Run the tests
```bash
dotnet test
```

---

## Seeded admin credentials

| Field | Value |
| ----- | ----- |
| Email | `admin@example.com` |
| Password | `Admin@123` |
| Role | `Admin` |

The admin (and the `Admin` / `User` roles) are seeded via `HasData` inside
`AppDbContext`, so they exist as soon as migrations are applied.

---

## API overview

| Method | Route | Auth | Description |
| ------ | ----- | ---- | ----------- |
| POST | `/api/users/register` | Anonymous | Register a new user |
| POST | `/api/users/login` | Anonymous | Log in → access + refresh token |
| POST | `/api/users/refresh` | Anonymous | Exchange a refresh token for a new pair |
| GET | `/api/users/me` | Authenticated | Current user's profile |
| GET | `/api/users/admin/users` | Admin | List users |
| POST | `/api/users/admin/users` | Admin | Create a user |
| DELETE | `/api/users/admin/users/{id}` | Admin | Soft-delete a user |
| POST | `/api/tasks` | Authenticated | Create a task (queued for background processing) |
| GET | `/api/tasks` | Authenticated | List the caller's tasks (priority, then date) |
| GET | `/api/tasks/{id}` | Authenticated | Get one of the caller's tasks (Redis-cached) |
| PUT | `/api/tasks/{id}/status` | Authenticated | Update task status (invalidates cache) |

---

## Assumptions made

- **Integer keys for Identity** (`IdentityUser<int>` / `IdentityRole<int>`) so a task's
  `UserId` is a simple `int`.
- **Two fixed roles**: `Admin` and `User`. New registrations get `User`; the seeded
  account is the only `Admin`.
- **JWT**: `ValidateIssuer` and `ValidateAudience` are disabled for simplicity (only the
  signing key is validated). Access token lifetime comes from `Jwt:ExpiresInMinutes`
  (default 60 min); refresh tokens live 7 days and are **rotated** on use (the old one is
  revoked).
- **Soft delete applies to users only.** Deleting a user sets `IsDeleted = true` (handled
  in the `SaveChanges` override, since Identity's `UserManager` deletes bypass the
  repository). A global query filter hides soft-deleted rows.
- **Duplicate-title rule** compares against the current **UTC** date.
- **Redis is optional.** Cache operations are best-effort; if Redis is unavailable, reads
  fall back to the database and the app does not fail.
- **Migrations run automatically at startup** (`context.Database.Migrate()`), which is
  convenient for review; in production you might apply migrations as a separate step.
- The **admin's seeded password hash** is a fixed, pre-computed Identity hash (required
  because `HasData` runs at model-build time, where no `UserManager` is available to hash
  a password).
- The **JWT signing key and DB password live in `appsettings.json`** for ease of review;
  in a real project these would move to User Secrets / environment variables.

---

## Architecture

- **Domain** — entities (`ApplicationUser`, `TaskItem`, `RefreshToken`, `BaseEntity`),
  enums (`Domain/Enums`), and repository/unit-of-work interfaces.
- **Application** — DTOs, service interfaces, business logic (`UserService`,
  `TaskService`), and typed exceptions.
- **Infrastructure** — EF Core `DbContext` (+ seeding, soft delete, audit dates), the
  generic repository & unit of work, Redis cache, background queue + worker, DI wiring.
- **API** — controllers, JWT + Swagger setup, exception-handling middleware.
- **Tests** — xUnit unit tests for the task service and soft-delete behavior.
