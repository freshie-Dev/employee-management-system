# EMS API — .NET 9

User management and JWT login added using existing SQL Server `EMS` database.

## Requirements
- .NET 9 SDK
- SQL Server with `EMS` schema already created (tables `UserAccounts`, `Roles`, `UserRoles`)
- Connection string in `appsettings.json` (`ConnectionStrings:DbConnection`) — verify server/instance before running.

## Start
From this project directory:

```powershell
dotnet restore
dotnet user-secrets init
$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:Key" "$key"
dotnet user-secrets set "Bootstrap:Username" "admin"
dotnet user-secrets set "Bootstrap:Password" "REPLACE-WITH-YOUR-OWN-LONG-RANDOM-PASSWORD"
dotnet run -- --bootstrap-admin
dotnet user-secrets remove "Bootstrap:Password"
dotnet user-secrets remove "Bootstrap:Username"
dotnet run
```

The bootstrap is a one-time command, **not** a public API route. It expects the seeded `SYSTEM_ADMIN` role.

## Routes
- `POST /api/auth/login` public. Body: `{ "username": "admin", "password": "..." }`
- `GET /api/users` admin
- `GET /api/users/{id}` admin
- `POST /api/users` admin. Body: `{ "username":"newuser", "password":"long-password", "roleIds":[4] }`
- `PATCH /api/users/{id}` admin. Body: `{ "username":"new-name" }`
- `PATCH /api/users/{id}/status` admin. Body: `{ "isActive": false }`
- `PUT /api/users/{id}/roles` admin. Body: `{ "roleIds":[4] }`

Send access tokens using `Authorization: Bearer <token>`. Access tokens expire in 15 minutes. Role changes and account disablement are checked on authenticated requests.

## Implementation notes
- DB-first: this code maps only the 3 auth/user tables, without changing your SQL schema.
- The new user initially has `EmployeeId = NULL`; employee onboarding comes next.
- No refresh-token persistence or password-reset workflow yet.
- The last-admin checks are best-effort; serialize these operations in a future production-hardening pass.
- Database unique-index concurrency may raise `DbUpdateException`; refine centralized exception handling before production.
- No .NET SDK was available in the build environment, so compilation/runtime testing must be done locally.
