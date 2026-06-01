# EduVibe LMS API

The .NET 8 backend behind the EduVibe Academy platform. Owns the database, the domain logic, the JWT issuer, the RBAC engine, and a Telegram bridge for visitor inquiries.

Serves two frontends:

| Frontend | URL | Repo |
|---|---|---|
| **LMS admin panel** (teacher / admin / student) | `http://localhost:3000` | [`k00h1nur/LastChange`](https://github.com/k00h1nur/LastChange) |
| **Marketing site** (anonymous visitor) | `http://localhost:5173` | [`k00h1nur/Edu-Vibe`](https://github.com/k00h1nur/Edu-Vibe) |

---

## The three-repo system

```
┌──────────────────┐   POST /api/VisitorMessages   ┌─────────────────────┐
│   Edu-Vibe       │ ────────────────────────────► │  edu-vibe-back      │
│ (marketing site) │       [AllowAnonymous]        │  (this repo)        │
│                  │                               │                     │
│  /contact        │                               │  • PostgreSQL       │
│  /demo-lesson    │                               │  • CQRS + MediatR   │
│  /mock-test      │ ◄──────── JWT + REST ──────── │  • JWT auth         │
│  /level-check    │                               │  • RBAC policies    │
└──────────────────┘                               │  • Telegram bridge  │
                                                    └──────────┬──────────┘
┌──────────────────┐   GET /api/* with JWT cookie             │
│  LastChange      │ ◄────────────────────────────────────────┘
│ (LMS admin/      │                                          ▲
│  teacher/        │   POST /api/Auth/login → JWT  ───────────┘
│  student)        │
│                  │   /admin/inquiries displays
│  /admin/*        │   anonymous submissions
│  /studentspanel/*│
└──────────────────┘
```

The marketing site posts visitor messages to the **anonymous** `/api/VisitorMessages` endpoint. The LMS admin signs in via `/api/Auth/login` and uses a JWT for everything afterwards.

---

## Tech stack

| | |
|---|---|
| **Runtime** | .NET 8 |
| **Architecture** | Clean Architecture: `LMS.Domain` → `LMS.Application` → `LMS.Infrastructure` → `LMS.WebApi` |
| **Pattern** | CQRS via [MediatR](https://github.com/jbogard/MediatR) 12 |
| **Database** | PostgreSQL via EF Core 8 (Npgsql) |
| **Auth** | JWT bearer (HS256), refresh tokens, role + permission claims |
| **Validation** | [FluentValidation](https://fluentvalidation.net/) 11 via pipeline behavior |
| **Logging** | Serilog |
| **Caching** | StackExchange.Redis (optional — falls back to in-memory) |
| **API docs** | Swashbuckle / Swagger UI at `/swagger` (Development only) |

---

## Quick start

### Prerequisites

- .NET 8 SDK
- PostgreSQL 14+ running locally (or via Docker)
- Optional: Redis for distributed caching

### 1. Configure

Edit `LMS.WebApi/appsettings.json` (or use env vars):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=eduvibe;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "<32+ bytes — anything random>",
    "Issuer": "EduVibe",
    "Audience": "EduVibeClients"
  },
  "Telegram": {
    "BotToken": "<from @BotFather>",
    "ChatId": "<group/channel/user id>"
  }
}
```

The Telegram block is **optional** — leave it out for dev and the notifier no-ops with a single warning log on first call.

### 2. Apply migrations

```powershell
dotnet ef database update -p LMS.Infrastructure -s LMS.WebApi
```

(If `dotnet ef` isn't installed: `dotnet tool install --global dotnet-ef`.)

### 3. Run

```powershell
cd LMS.WebApi
dotnet run
```

Console will print something like:

```
Now listening on: https://localhost:7041
Now listening on: http://localhost:5041
```

Use the http port for local dev unless you've trusted the dev cert (`dotnet dev-certs https --trust`).

### 4. CORS for the frontends

`Program.cs` needs to allow your frontend origins. Add:

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(
        "http://localhost:3000",  // LastChange admin
        "http://localhost:5173"   // Edu-Vibe marketing
     )
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// after var app = builder.Build():
app.UseCors();
```

---

## Default seeded admin

The first time you run migrations, the seeder creates:

| Email | Password | Roles |
|---|---|---|
| `director@eduvibe.local` | `ChangeMe123!` | `AcademyDirector`, `SuperAdmin` |

Change the password via `POST /api/Users/me/password` on first login.

---

## Project layout

```
LMS.Domain/            Domain entities + enums + base types. No external deps.
  Entities/             User, Class, Assignment, VisitorMessage, ...
  Common/BaseEntity     Audit fields (Id, CreatedAt, UpdatedAt) + domain events.

LMS.Application/        CQRS surface. One folder per feature.
  Features/
    Auth/               Login, Register, Refresh, AssignRole
    Students/           CRUD + /me lookup
    VisitorMessages/    Anonymous submit + admin inbox
    Classes/, Sessions/, Assignments/, Submissions/, Attendance/, ...
  Common/
    Abstractions/       IApplicationDbContext, ITelegramNotifier, ICurrentUserService
    Behaviors/          MediatR pipeline: Validation, Logging, Performance
    Security/           Permissions catalog + RolePermissionMatrix
    Models/Result<T>    Internal envelope; ApiResponse<T> wraps it on the wire.

LMS.Infrastructure/     EF + services. The only project that knows about Npgsql.
  Persistence/          LMSDbContext, EntityConfigurations, SeedData
  Migrations/           EF Core migrations
  Services/             JwtTokenGenerator, PasswordHasher, TelegramNotifier, ...

LMS.WebApi/             HTTP surface.
  Controllers/          Thin — each action dispatches to MediatR.
  Security/             PermissionAuthorizeAttribute, DynamicPolicyProvider,
                        PermissionAuthorizationHandler,
                        PermissionDiscoveryHostedService,
                        RolePermissionSeederHostedService
  Middleware/           GlobalExceptionMiddleware, RequestResponseLoggingMiddleware
  Common/ApiResponse.cs Standard HTTP envelope { success, message, data, errors }.
  Program.cs            Composition root.
```

---

## RBAC system

Permissions are **discovered**, **seeded**, and **enforced** — three coordinated pieces:

```
┌──────────────────────────────┐
│  Permissions.cs              │  Compile-time string constants
│  Permissions.Students.Read   │  referenced by [PermissionAuthorize].
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ PermissionDiscoveryHostedSvc │  Runs at startup, scans every controller,
│                              │  INSERTs missing rows into `permissions`.
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ RolePermissionSeederHostedSvc│  Bootstrap-once per role from
│                              │  RolePermissionMatrix. Once a role has any
│                              │  grants it's hands-off — admin edits persist.
└──────────────┬───────────────┘
               │
               ▼
┌───────────────────────────────┐
│ PermissionAuthorizationHandler│ Checks `permission` JWT claim first
│                               │  (fast path). DB fallback for missing claims.
│                               │  SuperAdmin always passes.
└───────────────────────────────┘
```

**Adding a new permission:**

1. Add a constant to `LMS.Application/Common/Security/Permissions.cs` (e.g. `Inventory.Read`).
2. Decorate the controller action: `[PermissionAuthorize(Permissions.Inventory.Read)]`.
3. (Optional) Add to `RolePermissionMatrix.ForOfficeAdmin` etc. if you want it granted by default to fresh roles.
4. Restart. `PermissionDiscoveryHostedService` writes the new row to `permissions`. Admins assign it to existing roles via `POST /api/admin/rbac/roles/{id}/permissions`.

---

## Roles + permission matrix

| Role | Default grants |
|---|---|
| `SuperAdmin` | Implicit bypass + all permissions explicitly. |
| `Admin` | All permissions explicitly. |
| `AcademyDirector` | Full read across academy, manage payments, oversee staff, view roles. |
| `OfficeAdmin` | Manage students/staff/classes/sessions/attendance/payments. Can assign roles. |
| `Teacher` | Own classes, students they teach, full assignment/submission/attendance workflow, award badges, grant XP. |
| `SupportTeacher` | Read-only across academy + mark attendance + send messages. |
| `Student` | Self-scoped: own assignments/submissions/badges + messaging + leaderboard. |

Full grant tables live in `Permissions.cs` → `RolePermissionMatrix`.

---

## Visitor messages + Telegram

Marketing site submissions land in the `visitor_messages` table. Four `Source` values:

| Source | From | Telegram label |
|---|---|---|
| 1 = Contact | `/contact` | ✉️ Contact Form |
| 2 = DemoLesson | `/demo-lesson` | 🎓 Demo Lesson Request |
| 3 = MockTest | `/mock-test` | 📝 Mock Test Registration |
| 4 = LevelCheck | `/level-check` | 🎯 Level Check Request |

The create handler fires `ITelegramNotifier.SendAsync` as **fire-and-forget** — Telegram down ≠ visitor sees an error. See `LMS.Infrastructure/Services/TelegramNotifier.cs`.

Admin inbox reads via `GET /api/VisitorMessages` (paginated, filterable by `isRead` + `source`).

---

## Common dev commands

```powershell
# Run with hot reload
cd LMS.WebApi
dotnet watch run

# Add a migration
dotnet ef migrations add MigrationName -p LMS.Infrastructure -s LMS.WebApi

# Apply pending migrations
dotnet ef database update -p LMS.Infrastructure -s LMS.WebApi

# Revert the last migration (before committing)
dotnet ef migrations remove -p LMS.Infrastructure -s LMS.WebApi

# Build everything
dotnet build
```

---

## Conventions

- **DTOs are sealed records**, immutable. Live in each feature's `*Contracts.cs`.
- **Commands / Queries**: sealed records implementing `IRequest<Result<T>>`.
- **Handlers**: sealed classes, inject `IApplicationDbContext` directly. Manual `.Select(x => new XDto(...))` projection — no AutoMapper / Mapster.
- **Validators**: FluentValidation in `*Validators.cs`. Auto-registered by the assembly scan in `LMS.Application/DependencyInjection.cs`.
- **Domain entities**: rich models with private setters + Update / Domain methods. **No reflection-based mutation** anywhere in handlers (see `Class.UpdateDetails`, `User.SetEmail`, etc.).
- **Controllers**: thin — `await sender.Send(command, ct)` then wrap in `ApiResponse<T>.Ok/Fail`. Most actions are 4 lines.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `Cannot reach … ECONNREFUSED` from a frontend | Backend isn't running, or wrong port | Start `dotnet run`; check the port in console output. |
| `UNABLE_TO_VERIFY_LEAF_SIGNATURE` | Self-signed dev cert isn't trusted | `dotnet dev-certs https --trust` or point frontends at the http port. |
| CORS preflight failure in browser | Origin not allowed | Add the frontend origin to the `AddCors` policy in `Program.cs`. |
| `INVALID_REFRESH_TOKEN` after server restart | Refresh tokens hashed with a salt that's process-local | Sign out + sign in again. |
| `Telegram notifier is disabled` warning on boot | `Telegram:BotToken` / `Telegram:ChatId` not set | Add them to `appsettings.json` or env vars. Or ignore — submissions still land in the DB. |
| `403 Forbidden` on a route the user should have access to | Role missing the permission grant | `POST /api/admin/rbac/roles/{roleId}/permissions` with the new permission id. |

---

## Notes / TODOs (no extra tables created)

- Support teacher many-to-many class assignment is TODO; current checks rely on `Class.TeacherUserId`.
- Recording / whiteboard / notifications / QR / calendar integrations are TODO service integrations.
- AI assignment generation should be implemented behind a service interface without adding tables.
- No rate limiting on `POST /api/VisitorMessages` — add a honeypot field or per-IP limit before going live publicly.

---

## Paired repositories

- **Admin / teacher / student LMS:** [`k00h1nur/LastChange`](https://github.com/k00h1nur/LastChange)
- **Public marketing site:** [`k00h1nur/Edu-Vibe`](https://github.com/k00h1nur/Edu-Vibe)
