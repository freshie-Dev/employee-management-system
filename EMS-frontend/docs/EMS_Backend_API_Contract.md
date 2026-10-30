# EMS Backend API Contract — ASP.NET Core 9

**Purpose:** API-only reference for integrating the Angular frontend.

**Base URL (development):** `https://localhost:7241/api`  
**Angular proxy path:** `/api`  
**Payloads:** JSON, camelCase  
**Authentication:** `Authorization: Bearer <accessToken>` on protected endpoints.

> **Verification note:** This contract reflects the API endpoints and DTO examples documented in our earlier EMS backend conversation. The current controller source was not re-inspected for this handoff; verify response status codes, optional fields, and authorization attributes against your running OpenAPI/Swagger definition. The assignment correction PUT endpoint was proposed separately and may not be implemented yet.

## 1. Authentication

### `POST /api/auth/login` — Public

Request:
```json
{"username":"admin","password":"your-password"}
```

Successful response (`200`):
```json
{
  "accessToken":"<JWT>",
  "tokenType":"Bearer",
  "expiresAt":"2026-10-21T10:30:00Z",
  "userId":1,
  "username":"admin",
  "roles":["SYSTEM_ADMIN"]
}
```

Failure: `401` for invalid credentials.

## 2. Users — `SYSTEM_ADMIN`

| Method | Endpoint | Body | Expected success |
|---|---|---|---|
| GET | `/api/users` | — | `200`, list of users |
| GET | `/api/users/{id}` | — | `200`, user |
| POST | `/api/users` | CreateUser | `201`, user |
| PATCH | `/api/users/{id}` | UpdateUser | `200`, user |
| PATCH | `/api/users/{id}/status` | UpdateStatus | `204` |
| PUT | `/api/users/{id}/roles` | UpdateRoles | `204` |

CreateUser:
```json
{"username":"employee.user","password":"StrongPassword123!","roleIds":[4]}
```

UpdateUser:
```json
{"username":"employee.updated"}
```

UpdateStatus:
```json
{"isActive":false}
```

UpdateRoles (**replace** existing roles):
```json
{"roleIds":[3,4]}
```

Representative user response:
```json
{
  "userId":2,
  "employeeId":null,
  "username":"employee.user",
  "isActive":true,
  "createdAt":"2026-10-21T10:15:00Z",
  "roles":["EMPLOYEE"]
}
```

Business constraints discussed: unique username; username 3–100 characters; password minimum 12 characters; supplied role IDs must exist; cannot deactivate self/change own roles or remove last active admin. **Verify current backend enforcement.**

## 3. Departments — `SYSTEM_ADMIN` or `HR_MANAGER`

| Method | Endpoint | Body | Expected success |
|---|---|---|---|
| GET | `/api/departments?search=IT` | — | `200`, department array |
| GET | `/api/departments/{id}` | — | `200`, department |
| POST | `/api/departments` | DepartmentRequest | `201`, department |
| PUT | `/api/departments/{id}` | DepartmentRequest | `200`, department |
| PATCH | `/api/departments/{id}/status` | UpdateStatus | `204` |

DepartmentRequest:
```json
{"name":"Information Technology","code":"IT","parentDepartmentId":null}
```

Department response:
```json
{"departmentId":1,"name":"Information Technology","code":"IT","parentDepartmentId":null,"isActive":true}
```

Status body: `{"isActive":false}`. Optional `search` searches names/codes; previously documented list limit: 200. Constraints: unique department code, no self-parent or circular hierarchy, active child restrictions on deactivation.

## 4. Designations — `SYSTEM_ADMIN` or `HR_MANAGER`

| Method | Endpoint | Body | Expected success |
|---|---|---|---|
| GET | `/api/designations` | — | `200`, designation array |
| GET | `/api/designations/{id}` | — | `200`, designation |
| POST | `/api/designations` | DesignationRequest | `201`, designation |
| PUT | `/api/designations/{id}` | DesignationRequest | `200`, designation |
| PATCH | `/api/designations/{id}/status` | UpdateStatus | `204` |

DesignationRequest:
```json
{"title":"Software Engineer","code":"SWE","description":"Software development"}
```

Designation response:
```json
{"designationId":1,"title":"Software Engineer","code":"SWE","description":"Software development","isActive":true}
```

Status body: `{"isActive":false}`. `code` must be unique. Previously documented list limit: 200.

## 5. Employees — `SYSTEM_ADMIN` or `HR_MANAGER`

| Method | Endpoint | Body | Expected success |
|---|---|---|---|
| GET | `/api/employees?search=ali&page=1&pageSize=20` | — | `200`, paginated response |
| GET | `/api/employees/{id}` | — | `200`, employee |
| POST | `/api/employees` | CreateEmployee | `201`, employee |
| PUT | `/api/employees/{id}` | UpdateEmployee | `200`, employee |
| PATCH | `/api/employees/{id}/status` | EmploymentStatusUpdate | `204` |
| PUT | `/api/employees/{id}/user` | UserLinkUpdate | `204` |

Pagination query: `search?: string`, `page?: number` (default `1`), `pageSize?: number` (default `20`, maximum `100`).

Paginated response:
```json
{
  "total":1,
  "page":1,
  "pageSize":20,
  "items":[{
    "employeeId":1,
    "userId":2,
    "employeeCode":"EMP-001",
    "firstName":"Ahmad",
    "lastName":"Ali",
    "workEmail":"ahmad@example.com",
    "employmentType":"PERMANENT",
    "employmentStatus":"ACTIVE",
    "hireDate":"2026-10-20T00:00:00"
  }]
}
```

CreateEmployee:
```json
{
  "userId":null,
  "employeeCode":"EMP-001",
  "firstName":"Ahmad",
  "lastName":"Ali",
  "workEmail":"ahmad@example.com",
  "personalEmail":null,
  "phoneNumber":"+923001234567",
  "dateOfBirth":"2000-05-15",
  "hireDate":"2026-10-20",
  "employmentType":"PERMANENT"
}
```

UpdateEmployee (does **not** update `employeeCode`, `hireDate` or `userId`):
```json
{
  "firstName":"Ahmad",
  "lastName":"Ali",
  "workEmail":"ahmad@example.com",
  "personalEmail":null,
  "phoneNumber":"+923001234567",
  "dateOfBirth":"2000-05-15",
  "employmentType":"PERMANENT"
}
```

EmploymentStatusUpdate:
```json
{"status":"ON_LEAVE"}
```

UserLinkUpdate (pass `null` to unlink):
```json
{"userId":2}
```

Enums:
- `employmentType`: `PERMANENT | CONTRACT | INTERN`
- `employmentStatus`: `ACTIVE | ON_LEAVE | TERMINATED`

An employee may exist without an associated user account. One user account cannot be linked to multiple employees.

## 6. Employee Assignments — `SYSTEM_ADMIN` or `HR_MANAGER`

| Method | Endpoint | Body | Expected success | Notes |
|---|---|---|---|---|
| GET | `/api/employees/{id}/assignments` | — | `200`, assignment array | Includes historical records |
| POST | `/api/employees/{id}/assignments` | AssignmentRequest | `200`, assignment | Creates new effective-dated row; closes prior one |
| GET | `/api/employees/{id}/reportees` | — | `200`, reportees | Response shape should be checked; previously returned EF entities |
| PUT | `/api/employees/{id}/assignments/{assignmentId}` | AssignmentRequest | `200`, assignment | **Proposed later; verify implementation** |

AssignmentRequest:
```json
{"departmentId":2,"designationId":3,"managerId":5,"effectiveFrom":"2026-10-21"}
```

Assignment response:
```json
{
  "assignmentId":2,
  "employeeId":1,
  "departmentId":2,
  "designationId":3,
  "managerId":5,
  "effectiveFrom":"2026-10-21T00:00:00",
  "effectiveTo":null
}
```

- **POST:** Creates a new assignment history entry; prior assignment is ended as appropriate. Prevents overlapping dates and invalid manager relationships.
- **PUT (if added):** Corrects a specific existing assignment in place; `GET assignments` will show the corrected values, but not the superseded values. Audit logging is deferred.
- `managerId` is an **EmployeeId**, not a UserId, and may be `null`.

## 7. Errors

| Status | Meaning |
|---|---|
| `400` | Invalid request or validation failure |
| `401` | Missing, invalid, or expired token; failed login |
| `403` | Authenticated but insufficient role |
| `404` | Record not found |
| `409` | Duplicate/conflicting business state |
| `500` | Unexpected server error |

Error body formats previously observed/discussed:
```json
{"message":"Username already exists."}
```

```json
"Department code exists."
```

ASP.NET validation errors may use `ValidationProblemDetails`:
```json
{"errors":{"Username":["The Username field is required."]}}
```

**Angular integration:** Handle object messages, plain string errors, and `errors` dictionaries; do not assume all endpoints return the same error envelope.

## 8. Not yet implemented / confirm separately

- `GET /api/roles` — not implemented in last known backend state.
- `GET /api/permissions` — not implemented.
- `POST /api/auth/refresh` — not implemented.
- `POST /api/auth/logout` — not implemented (client-side token removal only).
- `POST /api/auth/change-password` — not implemented.
- `GET /api/audit-logs` — not implemented; audit logging deferred.
- `PUT /api/employees/{id}/assignments/{assignmentId}` — discussed as an addition; confirm whether it was applied.

**Scope:** This document intentionally contains only the backend API contract. No frontend architecture, theming, or project handoff instructions.
