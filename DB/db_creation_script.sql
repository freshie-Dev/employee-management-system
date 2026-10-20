
CREATE DATABASE EMS;
GO

USE EMS;
GO

/* ============================================================
   1. ORGANIZATIONAL STRUCTURE
   ============================================================ */

CREATE TABLE dbo.Departments (
    DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Code VARCHAR(20) NOT NULL UNIQUE,
    ParentDepartmentId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Departments_Parent
        FOREIGN KEY (ParentDepartmentId)
        REFERENCES dbo.Departments(DepartmentId),

    CONSTRAINT CK_Departments_Parent
        CHECK (ParentDepartmentId IS NULL
            OR ParentDepartmentId <> DepartmentId)
);

CREATE TABLE dbo.Designations (
    DesignationId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME()
);

/* ============================================================
   2. EMPLOYEE MANAGEMENT
   ============================================================ */

CREATE TABLE dbo.Employees (
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode VARCHAR(30) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    WorkEmail NVARCHAR(255) NOT NULL UNIQUE,
    PersonalEmail NVARCHAR(255) NULL,
    PhoneNumber VARCHAR(30) NULL,
    DateOfBirth DATE NULL,
    HireDate DATE NOT NULL,
    EmploymentType VARCHAR(20) NOT NULL,
    EmploymentStatus VARCHAR(20) NOT NULL
        DEFAULT 'ACTIVE',
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT CK_Employees_Type
        CHECK (EmploymentType IN
            ('PERMANENT', 'CONTRACT', 'INTERN')),

    CONSTRAINT CK_Employees_Status
        CHECK (EmploymentStatus IN
            ('ACTIVE', 'ON_LEAVE', 'TERMINATED'))
);

/* Tracks department, designation and manager history */

CREATE TABLE dbo.EmployeeAssignments (
    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    DepartmentId INT NOT NULL,
    DesignationId INT NOT NULL,
    ManagerId INT NULL,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_EA_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_EA_Department
        FOREIGN KEY (DepartmentId)
        REFERENCES dbo.Departments(DepartmentId),

    CONSTRAINT FK_EA_Designation
        FOREIGN KEY (DesignationId)
        REFERENCES dbo.Designations(DesignationId),

    CONSTRAINT FK_EA_Manager
        FOREIGN KEY (ManagerId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT CK_EA_Dates
        CHECK (EffectiveTo IS NULL
            OR EffectiveTo >= EffectiveFrom),

    CONSTRAINT CK_EA_Manager
        CHECK (ManagerId IS NULL
            OR ManagerId <> EmployeeId),

    CONSTRAINT UQ_EA_Start
        UNIQUE (EmployeeId, EffectiveFrom)
);

CREATE UNIQUE INDEX UX_EA_Current
ON dbo.EmployeeAssignments(EmployeeId)
WHERE EffectiveTo IS NULL;

/* Hiring, resignation, termination, rejoining, etc. */

CREATE TABLE dbo.EmploymentEvents (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    EventType VARCHAR(30) NOT NULL,
    EffectiveDate DATE NOT NULL,
    Remarks NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_EmploymentEvents_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT CK_EmploymentEvents_Type
        CHECK (EventType IN (
            'HIRED',
            'CONFIRMED',
            'RESIGNED',
            'TERMINATED',
            'REHIRED',
            'RETIRED'
        ))
);

/* ============================================================
   3. AUTHENTICATION AND AUTHORIZATION
   ============================================================ */

CREATE TABLE dbo.UserAccounts (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastLoginAt DATETIME2(3) NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_UserAccounts_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId)
);

CREATE UNIQUE INDEX UX_UserAccounts_Employee
ON dbo.UserAccounts(EmployeeId)
WHERE EmployeeId IS NOT NULL;

CREATE TABLE dbo.Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE dbo.Permissions (
    PermissionId INT IDENTITY(1,1) PRIMARY KEY,
    PermissionKey VARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE dbo.UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,

    CONSTRAINT PK_UserRoles
        PRIMARY KEY (UserId, RoleId),

    CONSTRAINT FK_UserRoles_User
        FOREIGN KEY (UserId)
        REFERENCES dbo.UserAccounts(UserId),

    CONSTRAINT FK_UserRoles_Role
        FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles(RoleId)
);

CREATE TABLE dbo.RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,

    CONSTRAINT PK_RolePermissions
        PRIMARY KEY (RoleId, PermissionId),

    CONSTRAINT FK_RolePermissions_Role
        FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles(RoleId),

    CONSTRAINT FK_RolePermissions_Permission
        FOREIGN KEY (PermissionId)
        REFERENCES dbo.Permissions(PermissionId)
);

/* ============================================================
   4. EMPLOYEE DOCUMENTS
   ============================================================ */

CREATE TABLE dbo.DocumentTypes (
    DocumentTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE dbo.EmployeeDocuments (
    DocumentId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    DocumentTypeId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StorageKey NVARCHAR(500) NOT NULL,
    MimeType VARCHAR(100) NULL,
    FileSizeBytes BIGINT NULL,
    UploadedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Documents_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_Documents_Type
        FOREIGN KEY (DocumentTypeId)
        REFERENCES dbo.DocumentTypes(DocumentTypeId),

    CONSTRAINT CK_Documents_Size
        CHECK (FileSizeBytes IS NULL
            OR FileSizeBytes >= 0)
);

/* ============================================================
   5. ATTENDANCE AND SHIFTS
   ============================================================ */

CREATE TABLE dbo.Shifts (
    ShiftId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    BreakMinutes INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Shifts_Break
        CHECK (BreakMinutes >= 0)
);

CREATE TABLE dbo.EmployeeShiftAssignments (
    AssignmentId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ShiftId INT NOT NULL,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,

    CONSTRAINT FK_ESA_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_ESA_Shift
        FOREIGN KEY (ShiftId)
        REFERENCES dbo.Shifts(ShiftId),

    CONSTRAINT CK_ESA_Dates
        CHECK (EffectiveTo IS NULL
            OR EffectiveTo >= EffectiveFrom),

    CONSTRAINT UQ_ESA_Start
        UNIQUE (EmployeeId, EffectiveFrom)
);

CREATE UNIQUE INDEX UX_ESA_Current
ON dbo.EmployeeShiftAssignments(EmployeeId)
WHERE EffectiveTo IS NULL;

CREATE TABLE dbo.AttendanceRecords (
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ShiftId INT NULL,
    WorkDate DATE NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PRESENT',
    Remarks NVARCHAR(500) NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Attendance_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_Attendance_Shift
        FOREIGN KEY (ShiftId)
        REFERENCES dbo.Shifts(ShiftId),

    CONSTRAINT UQ_Attendance_Daily
        UNIQUE (EmployeeId, WorkDate),

    CONSTRAINT CK_Attendance_Status
        CHECK (Status IN (
            'PRESENT',
            'ABSENT',
            'LATE',
            'HALF_DAY',
            'LEAVE',
            'HOLIDAY'
        ))
);

CREATE TABLE dbo.AttendancePunches (
    PunchId INT IDENTITY(1,1) PRIMARY KEY,
    AttendanceId INT NOT NULL,
    PunchAt DATETIME2(3) NOT NULL,
    PunchType VARCHAR(3) NOT NULL,

    CONSTRAINT FK_Punches_Attendance
        FOREIGN KEY (AttendanceId)
        REFERENCES dbo.AttendanceRecords(AttendanceId),

    CONSTRAINT CK_Punches_Type
        CHECK (PunchType IN ('IN', 'OUT'))
);

CREATE TABLE dbo.HolidayCalendars (
    CalendarId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Holidays (
    HolidayId INT IDENTITY(1,1) PRIMARY KEY,
    CalendarId INT NOT NULL,
    HolidayDate DATE NOT NULL,
    Name NVARCHAR(150) NOT NULL,

    CONSTRAINT FK_Holidays_Calendar
        FOREIGN KEY (CalendarId)
        REFERENCES dbo.HolidayCalendars(CalendarId),

    CONSTRAINT UQ_Holidays_Date
        UNIQUE (CalendarId, HolidayDate)
);

/* ============================================================
   6. LEAVE MANAGEMENT
   ============================================================ */

CREATE TABLE dbo.LeaveTypes (
    LeaveTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    IsPaid BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.LeaveBalances (
    BalanceId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveTypeId INT NOT NULL,
    LeaveYear INT NOT NULL,
    OpeningDays DECIMAL(8,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_LeaveBalances_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_LeaveBalances_Type
        FOREIGN KEY (LeaveTypeId)
        REFERENCES dbo.LeaveTypes(LeaveTypeId),

    CONSTRAINT UQ_LeaveBalances
        UNIQUE (EmployeeId, LeaveTypeId, LeaveYear),

    CONSTRAINT CK_LeaveBalances_Year
        CHECK (LeaveYear BETWEEN 1900 AND 9999),

    CONSTRAINT CK_LeaveBalances_Opening
        CHECK (OpeningDays >= 0)
);

CREATE TABLE dbo.LeaveRequests (
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveTypeId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    RequestedDays DECIMAL(8,2) NOT NULL,
    Reason NVARCHAR(1000) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    RequestedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_LeaveRequests_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_LeaveRequests_Type
        FOREIGN KEY (LeaveTypeId)
        REFERENCES dbo.LeaveTypes(LeaveTypeId),

    CONSTRAINT CK_LeaveRequests_Dates
        CHECK (EndDate >= StartDate),

    CONSTRAINT CK_LeaveRequests_Days
        CHECK (RequestedDays > 0),

    CONSTRAINT CK_LeaveRequests_Status
        CHECK (Status IN (
            'PENDING',
            'APPROVED',
            'REJECTED',
            'CANCELLED'
        ))
);

CREATE TABLE dbo.LeaveApprovals (
    ApprovalId INT IDENTITY(1,1) PRIMARY KEY,
    RequestId INT NOT NULL,
    ReviewerEmployeeId INT NOT NULL,
    Decision VARCHAR(20) NOT NULL,
    Comments NVARCHAR(1000) NULL,
    ActionAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_LeaveApprovals_Request
        FOREIGN KEY (RequestId)
        REFERENCES dbo.LeaveRequests(RequestId),

    CONSTRAINT FK_LeaveApprovals_Reviewer
        FOREIGN KEY (ReviewerEmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT CK_LeaveApprovals_Decision
        CHECK (Decision IN (
            'APPROVED',
            'REJECTED',
            'RETURNED'
        ))
);

CREATE TABLE dbo.LeaveTransactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    BalanceId INT NOT NULL,
    RequestId INT NULL,
    DaysDelta DECIMAL(8,2) NOT NULL,
    TransactionType VARCHAR(20) NOT NULL,
    Remarks NVARCHAR(500) NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_LeaveTransactions_Balance
        FOREIGN KEY (BalanceId)
        REFERENCES dbo.LeaveBalances(BalanceId),

    CONSTRAINT FK_LeaveTransactions_Request
        FOREIGN KEY (RequestId)
        REFERENCES dbo.LeaveRequests(RequestId),

    CONSTRAINT CK_LeaveTransactions_Type
        CHECK (TransactionType IN (
            'ACCRUAL',
            'DEDUCTION',
            'ADJUSTMENT',
            'REVERSAL'
        )),

    CONSTRAINT CK_LeaveTransactions_Nonzero
        CHECK (DaysDelta <> 0)
);

/* ============================================================
   7. PAYROLL AND COMPENSATION
   ============================================================ */

CREATE TABLE dbo.SalaryComponents (
    ComponentId INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(30) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    ComponentType VARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_SalaryComponents_Type
        CHECK (ComponentType IN (
            'EARNING',
            'DEDUCTION'
        ))
);

CREATE TABLE dbo.EmployeeCompensations (
    CompensationId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Compensations_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT CK_Compensations_Dates
        CHECK (EffectiveTo IS NULL
            OR EffectiveTo >= EffectiveFrom),

    CONSTRAINT UQ_Compensations_Start
        UNIQUE (EmployeeId, EffectiveFrom)
);

CREATE UNIQUE INDEX UX_Compensations_Current
ON dbo.EmployeeCompensations(EmployeeId)
WHERE EffectiveTo IS NULL;

CREATE TABLE dbo.CompensationItems (
    ItemId INT IDENTITY(1,1) PRIMARY KEY,
    CompensationId INT NOT NULL,
    ComponentId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_CompItems_Compensation
        FOREIGN KEY (CompensationId)
        REFERENCES dbo.EmployeeCompensations(CompensationId),

    CONSTRAINT FK_CompItems_Component
        FOREIGN KEY (ComponentId)
        REFERENCES dbo.SalaryComponents(ComponentId),

    CONSTRAINT UQ_Compensation_Component
        UNIQUE (CompensationId, ComponentId),

    CONSTRAINT CK_CompItems_Amount
        CHECK (Amount >= 0)
);

CREATE TABLE dbo.PayrollPeriods (
    PeriodId INT IDENTITY(1,1) PRIMARY KEY,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,

    CONSTRAINT UQ_PayrollPeriods
        UNIQUE (StartDate, EndDate),

    CONSTRAINT CK_PayrollPeriods_Dates
        CHECK (EndDate >= StartDate)
);

CREATE TABLE dbo.PayrollRuns (
    RunId INT IDENTITY(1,1) PRIMARY KEY,
    PeriodId INT NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),
    FinalizedAt DATETIME2(3) NULL,

    CONSTRAINT FK_PayrollRuns_Period
        FOREIGN KEY (PeriodId)
        REFERENCES dbo.PayrollPeriods(PeriodId),

    CONSTRAINT CK_PayrollRuns_Status
        CHECK (Status IN (
            'DRAFT',
            'PROCESSING',
            'FINALIZED',
            'CANCELLED'
        ))
);

CREATE TABLE dbo.Payslips (
    PayslipId INT IDENTITY(1,1) PRIMARY KEY,
    RunId INT NOT NULL,
    EmployeeId INT NOT NULL,
    GrossAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    DeductionAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount AS
        (GrossAmount - DeductionAmount) PERSISTED,

    CONSTRAINT FK_Payslips_Run
        FOREIGN KEY (RunId)
        REFERENCES dbo.PayrollRuns(RunId),

    CONSTRAINT FK_Payslips_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT UQ_Payslips_EmployeeRun
        UNIQUE (RunId, EmployeeId),

    CONSTRAINT CK_Payslips_Amounts
        CHECK (
            GrossAmount >= 0
            AND DeductionAmount >= 0
        )
);

CREATE TABLE dbo.PayslipItems (
    PayslipItemId INT IDENTITY(1,1) PRIMARY KEY,
    PayslipId INT NOT NULL,
    ComponentId INT NOT NULL,
    ComponentName NVARCHAR(100) NOT NULL,
    ComponentType VARCHAR(20) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_PayslipItems_Payslip
        FOREIGN KEY (PayslipId)
        REFERENCES dbo.Payslips(PayslipId),

    CONSTRAINT FK_PayslipItems_Component
        FOREIGN KEY (ComponentId)
        REFERENCES dbo.SalaryComponents(ComponentId),

    CONSTRAINT CK_PayslipItems_Type
        CHECK (ComponentType IN (
            'EARNING',
            'DEDUCTION'
        )),

    CONSTRAINT CK_PayslipItems_Amount
        CHECK (Amount >= 0)
);

/* ============================================================
   8. PERFORMANCE MANAGEMENT
   ============================================================ */

CREATE TABLE dbo.ReviewCycles (
    CycleId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'DRAFT',

    CONSTRAINT CK_ReviewCycles_Dates
        CHECK (EndDate >= StartDate),

    CONSTRAINT CK_ReviewCycles_Status
        CHECK (Status IN (
            'DRAFT',
            'ACTIVE',
            'CLOSED'
        ))
);

CREATE TABLE dbo.PerformanceReviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    CycleId INT NOT NULL,
    EmployeeId INT NOT NULL,
    ReviewerEmployeeId INT NULL,
    OverallRating DECIMAL(4,2) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Reviews_Cycle
        FOREIGN KEY (CycleId)
        REFERENCES dbo.ReviewCycles(CycleId),

    CONSTRAINT FK_Reviews_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT FK_Reviews_Reviewer
        FOREIGN KEY (ReviewerEmployeeId)
        REFERENCES dbo.Employees(EmployeeId),

    CONSTRAINT UQ_Reviews_CycleEmployee
        UNIQUE (CycleId, EmployeeId),

    CONSTRAINT CK_Reviews_Rating
        CHECK (OverallRating IS NULL
            OR OverallRating BETWEEN 1 AND 5),

    CONSTRAINT CK_Reviews_Status
        CHECK (Status IN (
            'DRAFT',
            'IN_PROGRESS',
            'SUBMITTED',
            'COMPLETED'
        ))
);

CREATE TABLE dbo.ReviewGoals (
    GoalId INT IDENTITY(1,1) PRIMARY KEY,
    ReviewId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Weight DECIMAL(5,2) NOT NULL,
    Rating DECIMAL(4,2) NULL,

    CONSTRAINT FK_ReviewGoals_Review
        FOREIGN KEY (ReviewId)
        REFERENCES dbo.PerformanceReviews(ReviewId),

    CONSTRAINT CK_ReviewGoals_Weight
        CHECK (Weight > 0 AND Weight <= 100),

    CONSTRAINT CK_ReviewGoals_Rating
        CHECK (Rating IS NULL
            OR Rating BETWEEN 1 AND 5)
);

CREATE TABLE dbo.ReviewComments (
    CommentId INT IDENTITY(1,1) PRIMARY KEY,
    ReviewId INT NOT NULL,
    AuthorEmployeeId INT NOT NULL,
    CommentText NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_ReviewComments_Review
        FOREIGN KEY (ReviewId)
        REFERENCES dbo.PerformanceReviews(ReviewId),

    CONSTRAINT FK_ReviewComments_Author
        FOREIGN KEY (AuthorEmployeeId)
        REFERENCES dbo.Employees(EmployeeId)
);

/* ============================================================
   9. AUDIT LOGGING
   ============================================================ */

CREATE TABLE dbo.AuditLogs (
    AuditLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(100) NOT NULL,
    Action VARCHAR(20) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    CorrelationId UNIQUEIDENTIFIER NULL,
    OccurredAt DATETIME2(3) NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AuditLogs_User
        FOREIGN KEY (UserId)
        REFERENCES dbo.UserAccounts(UserId),

    CONSTRAINT CK_AuditLogs_Action
        CHECK (Action IN (
            'INSERT',
            'UPDATE',
            'DELETE'
        )),

    CONSTRAINT CK_AuditLogs_OldJSON
        CHECK (OldValues IS NULL
            OR ISJSON(OldValues) = 1),

    CONSTRAINT CK_AuditLogs_NewJSON
        CHECK (NewValues IS NULL
            OR ISJSON(NewValues) = 1)
);

/* ============================================================
   10. INDEXES
   ============================================================ */

CREATE INDEX IX_Employees_Status
ON dbo.Employees(EmploymentStatus);

CREATE INDEX IX_EA_Department
ON dbo.EmployeeAssignments(DepartmentId);

CREATE INDEX IX_EA_Designation
ON dbo.EmployeeAssignments(DesignationId);

CREATE INDEX IX_EA_Manager
ON dbo.EmployeeAssignments(ManagerId);

CREATE INDEX IX_EA_Employee_Dates
ON dbo.EmployeeAssignments(
    EmployeeId, EffectiveFrom, EffectiveTo
);

CREATE INDEX IX_EmploymentEvents_Employee
ON dbo.EmploymentEvents(EmployeeId, EffectiveDate);

CREATE INDEX IX_UserRoles_Role
ON dbo.UserRoles(RoleId);

CREATE INDEX IX_RolePermissions_Permission
ON dbo.RolePermissions(PermissionId);

CREATE INDEX IX_Documents_Employee
ON dbo.EmployeeDocuments(EmployeeId);

CREATE INDEX IX_ESA_Employee_Dates
ON dbo.EmployeeShiftAssignments(
    EmployeeId, EffectiveFrom, EffectiveTo
);

CREATE INDEX IX_Attendance_WorkDate
ON dbo.AttendanceRecords(WorkDate);

CREATE INDEX IX_Punches_Attendance_Time
ON dbo.AttendancePunches(AttendanceId, PunchAt);

CREATE INDEX IX_LeaveRequests_Employee
ON dbo.LeaveRequests(EmployeeId, StartDate);

CREATE INDEX IX_LeaveRequests_Status
ON dbo.LeaveRequests(Status);

CREATE INDEX IX_LeaveApprovals_Request
ON dbo.LeaveApprovals(RequestId);

CREATE INDEX IX_LeaveTransactions_Balance
ON dbo.LeaveTransactions(BalanceId);

CREATE INDEX IX_LeaveTransactions_Request
ON dbo.LeaveTransactions(RequestId);

CREATE INDEX IX_Compensations_Employee_Dates
ON dbo.EmployeeCompensations(
    EmployeeId, EffectiveFrom, EffectiveTo
);

CREATE INDEX IX_PayrollRuns_Period
ON dbo.PayrollRuns(PeriodId);

CREATE INDEX IX_Payslips_Employee
ON dbo.Payslips(EmployeeId);

CREATE INDEX IX_PayslipItems_Component
ON dbo.PayslipItems(ComponentId);

CREATE INDEX IX_Reviews_Employee
ON dbo.PerformanceReviews(EmployeeId);

CREATE INDEX IX_Reviews_Reviewer
ON dbo.PerformanceReviews(ReviewerEmployeeId);

CREATE INDEX IX_ReviewGoals_Review
ON dbo.ReviewGoals(ReviewId);

CREATE INDEX IX_ReviewComments_Review
ON dbo.ReviewComments(ReviewId);

CREATE INDEX IX_AuditLogs_Entity
ON dbo.AuditLogs(EntityName, EntityId);

CREATE INDEX IX_AuditLogs_OccurredAt
ON dbo.AuditLogs(OccurredAt);

CREATE INDEX IX_AuditLogs_User
ON dbo.AuditLogs(UserId);

/* ============================================================
   11. DEFAULT ROLES
   ============================================================ */

INSERT INTO dbo.Roles (Name, Description)
VALUES
('SYSTEM_ADMIN', 'Full system administration'),
('HR_MANAGER', 'Employee and HR management'),
('MANAGER', 'Department and team management'),
('EMPLOYEE', 'Employee self-service');

/* ============================================================
   12. DEFAULT PERMISSIONS
   ============================================================ */

INSERT INTO dbo.Permissions
    (PermissionKey, Description)
VALUES
('employees.create', 'Create employee records'),
('employees.read', 'View employee records'),
('employees.update', 'Update employee records'),
('employees.delete', 'Deactivate employee records'),

('departments.manage', 'Manage departments'),
('designations.manage', 'Manage designations'),

('attendance.read', 'View attendance'),
('attendance.manage', 'Manage attendance'),

('leave.request', 'Submit leave requests'),
('leave.approve', 'Approve or reject leave requests'),
('leave.manage', 'Manage leave configuration'),

('payroll.read', 'View payroll'),
('payroll.manage', 'Manage payroll'),

('performance.read', 'View performance reviews'),
('performance.manage', 'Manage performance reviews'),

('users.manage', 'Manage user accounts'),
('roles.manage', 'Manage roles and permissions'),

('reports.view', 'View reports');

/* ============================================================
   13. VERIFICATION
   ============================================================ */

SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

SELECT
    COUNT(*) AS TotalTables
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_SCHEMA = 'dbo';
GO
