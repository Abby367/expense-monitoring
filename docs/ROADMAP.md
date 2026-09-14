# Expense Monitoring — Backend Roadmap

Progress tracker for the from-scratch rebuild. Update the status column as you go.

**Status key:** `TODO` · `WIP` · `DONE` · `BLOCKED` · `DEFERRED`

Last updated: 2026-09-14

---

## Where things stand

| | |
|---|---|
| Branch | `feat/user-soft-delete-guards` (off `dev`) |
| Last commit | `ce32d78` Make RemovedAt the single source of truth for removed users |
| Build | 0 errors, 0 warnings |
| Tests | 8 passing — `UserTests` (1), `PasswordHasherTests` (7) |
| Database | `safc_expense_v2`, 2 migrations applied, no model drift |
| Endpoints | `GET /api/health`, `GET /api/health/db`, `POST /api/v1/users` |

---

## Epic 1 — User & auth domain model · DONE

| # | Task | Status |
|---|---|---|
| 1.1 | `User` entity, encapsulated model, static factories | DONE |
| 1.2 | `RefreshToken` entity | DONE |
| 1.3 | EF configurations, `ExpenseDbContext`, DI registration | DONE |
| 1.4 | Initial migration, applied to Postgres | DONE |
| 1.5 | Soft delete — `RemovedAt` as single source of truth | DONE |
| 1.6 | Filtered unique index on Email (`RemovedAt IS NULL`) + migration | DONE |
| 1.7 | `IsRemoved` + `EnsureNotRemoved()` on all six public mutators | DONE |
| 1.8 | Test project wired to Domain; first real test | DONE |

---

## Epic 2 — First vertical slice: `POST /api/v1/users` · DONE

The point: nothing here has ever run. Each step is runnable and provable on its own.
**Do not write several steps then debug** — one at a time, so every bug has one possible cause.

| # | Step | File | Prove it by | Est | Status |
|---|---|---|---|---|---|
| 2.1 | `GET /api/health` | new `Api/Controllers/HealthController.cs` | 200 from the `.http` file; `Health` group appears in Swagger; `/api/healthz` gives 404 | 15m | DONE |
| 2.2 | Throw on missing connection string + DB health action | `Infrastructure/DependencyInjection.cs:15`, `HealthController` | `Users.CountAsync()` round-trips; blank the connection string and it dies at *startup* | 30m | DONE |
| 2.3 | `AddApplication()` + validator registration | new `Application/DependencyInjection.cs` | Temporarily inject `IValidator<CreateUserRequest>` into Health, confirm non-null, delete | 20m | DONE |
| 2.4 | `IPasswordHasher` with BCrypt (work factor 12) | new `Infrastructure/Authentication/PasswordHasher.cs` | Unit test — hash/verify round-trips, wrong password rejected | 30m | DONE |
| 2.5 | `CreateUserHandler` | new `Application/Users/CreateUser/` — Command, Handler, Response, Validator | Can't prove alone. Build clean; 2.6 proves it | 1h | DONE |
| 2.6 | `UsersController`, `POST /api/v1/users` | new `Api/Controllers/UsersController.cs` | 201 — then **look at the row in Postgres**: v7 Guid, lower-cased email, `Status=1`, `MustChangePassword=true`, expiry +48h | 45m | DONE |
| 2.7 | `IExceptionHandler` + ProblemDetails | new `Api/Middlewares/` | POST the same email twice → clean 409, no stack trace. Empty body → 400 with per-field messages | 45m | DONE |

### Architecture decisions locked in for this epic

| Decision | Choice | Why |
|---|---|---|
| Handler style | Plain injected handler classes, **no MediatR** — but adopt its folder shape | One use case; a pipeline with nothing to pipe. MediatR is commercially licensed now. Converting later is find-and-replace, not a rewrite. |
| Repositories | **No** | `DbSet<T>` is a repository, `SaveChangesAsync` is a unit of work. Onboarding wraps them; don't copy that. |
| `Result<T>` | **No, throw instead** | Needs a whole SharedKernel before the first feature. Revisit at 5 use cases. Going halfway is the thing that actually hurts. |
| API versioning | `api/v1/...` as a **plain literal**, no package | Adding a version segment later breaks every frontend call. |
| Validation | Explicit `await ValidateAsync(...)`, first line of `Handle` | See traps below. |

### Traps in this epic

- **Never call `.Validate()`** — only `ValidateAsync()`. `MustAsync` at `CreateUserValidator.cs:20` makes the sync path throw at runtime.
- **Do not install auto-validation** — it hooks MVC's synchronous pipeline and hits the same trap. Deprecated by FluentValidation's own maintainers.
- **BCrypt truncates at 72 bytes.** `ChangePasswordRequestValidator.cs:19` allows 128, so two passwords sharing their first 72 bytes are the same password.
- **Validators must be scoped**, never singleton — `CreateUserRequestValidator` captures `IExpenseDbContext`.
- **`[AllowAnonymous]` on 2.6 is the most dangerous line in the slice.** Anyone reaching the port can mint an account. `// TODO: remove when JWT lands`, and consider gating to Development.
- **DB health should report healthy/unhealthy, not the user count** — an anonymous endpoint publishing your user count is free reconnaissance.

---

## Epic 3 — Seed the superadmin · TODO

Onboarding's `UtilitiesController` + `PopulateDefaults` pattern: startup migrates only, an
`[AllowAnonymous]` POST seeds and returns a report. Idempotent.

`.env` already holds `Seed__SuperAdmin__Email/Password/FullName` and `Seed__Administrator__*`.
`.env.example` has only the connection string — it needs these keys (no real values).

Read first: `OnBoarding.WebApi/Controllers/UtilitiesController.cs:85-139`, and its
`CheckConfirmation` guard at `:52-74`.

---

## Epic 4 — Login + JWT · TODO

| # | Task | Notes |
|---|---|---|
| 4.1 | `ITokenService` implemented | Trim it first — `CreateSecureToken()` has no caller and overlaps `CreateRefreshToken()` |
| 4.2 | JWT options bound and validated at startup | |
| 4.3 | `UseAuthentication()` **before** `UseAuthorization()` | `Program.cs:37` currently enforces nothing |
| 4.4 | Refresh must re-check the user | A removed/suspended user's tokens still work today — `RefreshToken` has no query filter and no link to `User.RemovedAt`. Fix it here, inside this epic. |
| 4.5 | Remove `[AllowAnonymous]` from `POST /api/v1/users` | |

---

## Epic 5 — Roles & permissions · TODO

**Deliberately third.** `ITokenService.CreateAccessToken(User, IReadOnlyCollection<string> permissionClaims)`
already commits to "permissions are flat strings on the token" — a contract signed before it was
implemented. Build the token, feel where claims actually go, then shape the tables.

**Blocker to resolve now, not later:** `origin/feature/admin-console` (`9f9fc67`) — the complete v1
app and the reference for this feature — **is not on the remote and not in the local object store.**
`git ls-remote --heads origin` returns only `dev` and `main`. Ask whether anyone has a clone with
that branch. Time-sensitive in a way the code is not.

Before starting: pick the permission string format once. It is a three-way contract — database rows,
`[Authorize(Policy = ...)]` attributes, and frontend menu gating. Onboarding uses `"Can.View.Roles"`.

---

## Open decisions

| # | Question | Status |
|---|---|---|
| D1 | Who creates the first user? | **DECIDED** — seed a superadmin (Epic 3) |
| D2 | Removal: `Status` frozen vs. flipped? | **DECIDED** — Option B: `Status` describes a live account, `RemovedAt` is the only removal fact |
| D3 | Can a removed user's email be reused? | **DECIDED** — yes; filtered unique index |
| D4 | Does the API return the temporary password, or does email carry it? | OPEN — nothing sends email yet, so without it nobody can log in. Fallback: return it in the 201 body, Development only, named so nobody mistakes it for permanent. Note it lands in server logs, proxy logs and devtools history. |
| D5 | Does `AuthMethod.Microsoft` survive the rebuild? | OPEN — if Entra is out of scope, `MicrosoftId`, its index and the second factory all stop earning their keep |
| D6 | Scoped role grants? | OPEN — `UserContracts.cs:26` has `RoleGrantRequest(Guid RoleId, string? Scope = null)`, line 12 has a plain grant. Scoped roughly doubles the authorization work. **Delete `Scope` unless you can name the requirement it serves.** One keystroke now; a migration and a release later. |
| D7 | Is a removed user ever restored? | OPEN — `Activate()` now refuses, so removal is terminal by construction. A `Restore()` is complicated by the filtered index: the email may belong to a live account by then. |

---

## Backlog — real, but not now

| Item | Why not now |
|---|---|
| `MicrosoftId` index missing the `RemovedAt IS NULL` filter | The column is never assigned by any code path. Fold the one-line fix into the next migration generated for another reason. |
| `MicrosoftId` has no writer at all | Decide at Epic 4/5 whether it is set at creation or learned on first Microsoft sign-in. |
| `UserResponse.Roles` is permanently `[]` | Don't ship a field that lies to the frontend. Add it when roles exist. |
| `RefreshToken` uses `DateTimeOffset.UtcNow` internally | `User` takes `now` as a parameter. Two clock conventions; pick the injected one. Cosmetic until Epic 4. |
| `RemovedById` / `CreatedByUserId` have no FK | Self-referencing, and must be `Restrict` not `Cascade`. Fold into the next migration. |
| Reserve enum value `4` in `UserStatus` | `// 4 was Removed`. Table is empty today, so no data risk — but a restored dump would inherit the meaning. |
| Pin enum ints with a test | `Assert.Equal(2, (int)UserStatus.Active);` — `Status` is stored as an int with no `HasConversion`. Never renumber. |
| Length rules missing from validators | `CreateUserValidator` never checks `MaximumLength` despite the consts existing, so a 300-char email is a 500 not a 400. |
| `Application/User/` folder vs `...Application.Users` namespace | Also `CreateUserValidator.cs` contains `CreateUserRequestValidator`. Rename during Epic 2.5, when the folder moves anyway. |
| `.gitignore` diff strips explanatory comments | Uncommitted. Revert or commit separately — unrelated to the soft-delete work. |
| `CLAUDE.md` is stale | Says the backend "does not compile" and that `UserRole.cs` is empty. Neither is true; that file doesn't exist. |
| `CLAUDE.md` / `AGENTS.md` are gitignored | So every architecture decision above lives only on one machine. This file is the committed home for them. |
| No `.gitattributes` | LF/CRLF warnings on every `git add`. |

---

## The working loop

| Say | What happens |
|---|---|
| **next** | `senior` decides what to build and in what order |
| **check** / **done** | Claude reads your edits, reviews inline, builds |
| **review** | `code-review`, then `tutor` on the findings |
| **test** | `qa` runs the suite and specifies missing cases |
| **plan** | `plan` designs a feature across all four layers |
| a how/why question | `tutor` |
| **build** | just build |

You type every line. No agent edits files.
