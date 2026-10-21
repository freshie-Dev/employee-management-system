# EMS .NET 9 — New modules

This project extends the existing authentication, administrator bootstrap, and user management code. It uses existing tables in the EMS SQL Server schema; no schema creation or migration is performed.

## Development

1. Open `EMS.csproj` in Visual Studio or run `dotnet restore` then `dotnet run` in the folder containing `EMS.csproj`.
2. Ensure `DbConnection` points to the SQL Server `EMS` database created earlier (with `Departments`, `Designations`, `Employees`, and `EmployeeAssignments` tables).
3. Configure `Jwt:Key` through .NET user secrets (existing instructions in README).
4. Log in via `/api/auth/login`, authorize Swagger with a `SYSTEM_ADMIN` or `HR_MANAGER` token, then test endpoints below.

## Endpoints

- `/api/departments` GET, POST; `/{id}` GET, PUT; `/{id}/status` PATCH
- `/api/designations` GET, POST; `/{id}` GET, PUT; `/{id}/status` PATCH
- `/api/employees` GET (search/page/pageSize), POST; `/{id}` GET, PUT; `/{id}/status` PATCH
- `/api/employees/{id}/user` PUT to link or unlink an existing account, body `{ "userId": 2 }` or `{ "userId": null }`
- `/api/employees/{id}/assignments` GET and POST
- `/api/employees/{id}/reportees` GET

Departments and designations must exist before creating an employee assignment. Create the employee profile first, optionally link an existing user account, then assign an active department, designation and reporting manager. Employee history is preserved by closing the open-ended assignment and creating a new one.

## Example requests

Create department: `{ "name": "Engineering", "code": "ENG", "parentDepartmentId": null }`

Create designation: `{ "title": "Software Engineer", "code": "SWE", "description": "Software development" }`

Create employee: `{ "userId": 2, "employeeCode": "EMP-001", "firstName": "Ahmad", "lastName": "Ali", "workEmail": "ahmad@example.com", "hireDate": "2026-10-20", "employmentType": "PERMANENT" }`

Create assignment: `{ "departmentId": 1, "designationId": 1, "managerId": null, "effectiveFrom": "2026-10-20" }`

## Notes

- The existing user/auth endpoints remain unchanged.
- Authorization is initially role-based: organizational and employee management require SYSTEM_ADMIN or HR_MANAGER; department/designation reads allow any authenticated user.
- The initial workflow validates reporting loops and prevents overlapping assignments for the edited employee; concurrent writes, cross-employee effective-dated hierarchy logic, and historical payroll-grade integrity still need stronger transactional/business validation.
- Models map to SQL Server tables, without database migrations. No NuGet version upgrades are included.
- Not compiled in this environment: .NET SDK is unavailable. Run `dotnet build` locally.
