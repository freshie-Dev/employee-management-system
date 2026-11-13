
USE EMS;
GO

-- =============================================
-- 1. LIST EMPLOYEE SCHEDULE OVERRIDES
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_ListByEmployee
    @EmployeeId INT,
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @IncludeCancelled BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Employees
        WHERE EmployeeId = @EmployeeId
    )
        THROW 51301, 'Employee not found.', 1;

    SELECT
        OverrideId, EmployeeId, WorkDate, OverrideType,
        StartAt, EndAt, BreakMinutes, GraceInMinutes,
        GraceOutMinutes, RequiredMinutes, Reason,
        AssignedByUserId, RecordStatus,
        CreatedAt, UpdatedAt, RowVersion
    FROM dbo.EmployeeScheduleOverrides
    WHERE EmployeeId = @EmployeeId
      AND (@FromDate IS NULL OR WorkDate >= @FromDate)
      AND (@ToDate IS NULL OR WorkDate <= @ToDate)
      AND (
          @IncludeCancelled = 1
          OR RecordStatus = 'ACTIVE'
      )
    ORDER BY WorkDate DESC, OverrideId DESC;
END;
GO

-- =============================================
-- 2. GET OVERRIDE BY ID
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_GetById
    @EmployeeId INT,
    @OverrideId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        OverrideId, EmployeeId, WorkDate, OverrideType,
        StartAt, EndAt, BreakMinutes, GraceInMinutes,
        GraceOutMinutes, RequiredMinutes, Reason,
        AssignedByUserId, RecordStatus,
        CreatedAt, UpdatedAt, RowVersion
    FROM dbo.EmployeeScheduleOverrides
    WHERE EmployeeId = @EmployeeId
      AND OverrideId = @OverrideId;
END;
GO

-- =============================================
-- 3. CREATE SCHEDULE OVERRIDE
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Create
    @EmployeeId INT,
    @WorkDate DATE,
    @OverrideType VARCHAR(12),
    @StartAt DATETIMEOFFSET(0) = NULL,
    @EndAt DATETIMEOFFSET(0) = NULL,
    @BreakMinutes INT = 0,
    @GraceInMinutes INT = 0,
    @GraceOutMinutes INT = 0,
    @RequiredMinutes INT = NULL,
    @Reason NVARCHAR(500) = NULL,
    @AssignedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @WorkDate IS NULL
       OR @OverrideType NOT IN ('WORK', 'DAY_OFF')
       OR @OverrideType IS NULL
        THROW 51303, 'Invalid date or override type.', 1;

    IF @BreakMinutes NOT BETWEEN 0 AND 1440
       OR @GraceInMinutes NOT BETWEEN 0 AND 240
       OR @GraceOutMinutes NOT BETWEEN 0 AND 240
       OR (
           @RequiredMinutes IS NOT NULL
           AND @RequiredMinutes NOT BETWEEN 0 AND 1440
       )
        THROW 51303, 'Invalid break/grace/required minutes.', 1;

    IF (
        @OverrideType = 'DAY_OFF'
        AND (
            @StartAt IS NOT NULL
            OR @EndAt IS NOT NULL
            OR @BreakMinutes <> 0
            OR @RequiredMinutes IS NOT NULL
        )
    )
    OR (
        @OverrideType = 'WORK'
        AND (@StartAt IS NULL OR @EndAt IS NULL)
    )
        THROW 51303,
            'DAY_OFF must have no times; WORK requires start and end.',
            1;

    IF @OverrideType = 'WORK'
    BEGIN
        DECLARE @Span INT = DATEDIFF(
            MINUTE, @StartAt, @EndAt
        );

        IF @Span NOT BETWEEN 1 AND 1440
           OR @BreakMinutes >= @Span
           OR (
               @RequiredMinutes IS NOT NULL
               AND @RequiredMinutes > @Span - @BreakMinutes
           )
           OR CONVERT(DATE, @StartAt) <> @WorkDate
            THROW 51303,
                'Invalid work interval, duration or starting work date.',
                1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LockResult INT;

        DECLARE @LockResource NVARCHAR(255) =
            CONCAT(N'EMS.ScheduleOverride.', @EmployeeId);

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 51304,
                'Could not obtain schedule lock.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Employees
            WHERE EmployeeId = @EmployeeId
        )
            THROW 51301, 'Employee not found.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.UserAccounts
            WHERE UserId = @AssignedByUserId
        )
            THROW 51307, 'Assigning user not found.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.EmployeeScheduleOverrides
                WITH (UPDLOCK, HOLDLOCK)
            WHERE EmployeeId = @EmployeeId
              AND WorkDate = @WorkDate
              AND RecordStatus = 'ACTIVE'
        )
            THROW 51304,
                'An active override already exists for this work date.',
                1;

        IF @OverrideType = 'WORK'
           AND EXISTS (
               SELECT 1
               FROM dbo.EmployeeScheduleOverrides
                   WITH (UPDLOCK, HOLDLOCK)
               WHERE EmployeeId = @EmployeeId
                 AND RecordStatus = 'ACTIVE'
                 AND OverrideType = 'WORK'
                 AND StartAt < @EndAt
                 AND EndAt > @StartAt
           )
            THROW 51304,
                'Work interval overlaps an existing active override.',
                1;

        INSERT INTO dbo.EmployeeScheduleOverrides
        (
            EmployeeId, WorkDate, OverrideType,
            StartAt, EndAt, BreakMinutes,
            GraceInMinutes, GraceOutMinutes,
            RequiredMinutes, Reason, AssignedByUserId
        )
        VALUES
        (
            @EmployeeId, @WorkDate, @OverrideType,
            @StartAt, @EndAt, @BreakMinutes,
            @GraceInMinutes, @GraceOutMinutes,
            @RequiredMinutes, @Reason, @AssignedByUserId
        );

        DECLARE @Id BIGINT =
            CONVERT(BIGINT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        EXEC dbo.usp_ScheduleOverride_GetById
            @EmployeeId, @Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 4. UPDATE SCHEDULE OVERRIDE
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Update
    @EmployeeId INT,
    @OverrideId BIGINT,
    @ExpectedRowVersion BINARY(8),
    @OverrideType VARCHAR(12),
    @StartAt DATETIMEOFFSET(0) = NULL,
    @EndAt DATETIMEOFFSET(0) = NULL,
    @BreakMinutes INT = 0,
    @GraceInMinutes INT = 0,
    @GraceOutMinutes INT = 0,
    @RequiredMinutes INT = NULL,
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @WorkDate DATE;

    SELECT @WorkDate = WorkDate
    FROM dbo.EmployeeScheduleOverrides
    WHERE EmployeeId = @EmployeeId
      AND OverrideId = @OverrideId;

    IF @WorkDate IS NULL
        THROW 51302, 'Override not found.', 1;

    IF @OverrideType NOT IN ('WORK', 'DAY_OFF')
       OR @OverrideType IS NULL
       OR @BreakMinutes NOT BETWEEN 0 AND 1440
       OR @GraceInMinutes NOT BETWEEN 0 AND 240
       OR @GraceOutMinutes NOT BETWEEN 0 AND 240
       OR (
           @RequiredMinutes IS NOT NULL
           AND @RequiredMinutes NOT BETWEEN 0 AND 1440
       )
        THROW 51303, 'Invalid override values.', 1;

    IF (
        @OverrideType = 'DAY_OFF'
        AND (
            @StartAt IS NOT NULL
            OR @EndAt IS NOT NULL
            OR @BreakMinutes <> 0
            OR @RequiredMinutes IS NOT NULL
        )
    )
    OR (
        @OverrideType = 'WORK'
        AND (@StartAt IS NULL OR @EndAt IS NULL)
    )
        THROW 51303,
            'DAY_OFF must have no times; WORK requires start and end.',
            1;

    IF @OverrideType = 'WORK'
    BEGIN
        DECLARE @Span INT =
            DATEDIFF(MINUTE, @StartAt, @EndAt);

        IF @Span NOT BETWEEN 1 AND 1440
           OR @BreakMinutes >= @Span
           OR (
               @RequiredMinutes IS NOT NULL
               AND @RequiredMinutes > @Span - @BreakMinutes
           )
           OR CONVERT(DATE, @StartAt) <> @WorkDate
            THROW 51303, 'Invalid work interval or date.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LockResult INT;

        DECLARE @LockResource NVARCHAR(255) =
            CONCAT(N'EMS.ScheduleOverride.', @EmployeeId);

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 51304,
                'Could not obtain schedule lock.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.EmployeeScheduleOverrides
                WITH (UPDLOCK, HOLDLOCK)
            WHERE OverrideId = @OverrideId
              AND EmployeeId = @EmployeeId
              AND RecordStatus = 'ACTIVE'
        )
            THROW 51306,
                'Override is missing or cancelled.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.EmployeeScheduleOverrides
            WHERE OverrideId = @OverrideId
              AND EmployeeId = @EmployeeId
              AND RowVersion = @ExpectedRowVersion
        )
            THROW 51305,
                'Override changed since it was retrieved.', 1;

        IF @OverrideType = 'WORK'
           AND EXISTS (
               SELECT 1
               FROM dbo.EmployeeScheduleOverrides
                   WITH (UPDLOCK, HOLDLOCK)
               WHERE EmployeeId = @EmployeeId
                 AND OverrideId <> @OverrideId
                 AND RecordStatus = 'ACTIVE'
                 AND OverrideType = 'WORK'
                 AND StartAt < @EndAt
                 AND EndAt > @StartAt
           )
            THROW 51304,
                'Work interval overlaps another override.', 1;

        UPDATE dbo.EmployeeScheduleOverrides
        SET
            OverrideType = @OverrideType,
            StartAt = @StartAt,
            EndAt = @EndAt,
            BreakMinutes = @BreakMinutes,
            GraceInMinutes = @GraceInMinutes,
            GraceOutMinutes = @GraceOutMinutes,
            RequiredMinutes = @RequiredMinutes,
            Reason = @Reason,
            UpdatedAt = SYSUTCDATETIME()
        WHERE OverrideId = @OverrideId
          AND EmployeeId = @EmployeeId
          AND RowVersion = @ExpectedRowVersion;

        IF @@ROWCOUNT <> 1
            THROW 51305, 'Override changed concurrently.', 1;

        COMMIT TRANSACTION;

        EXEC dbo.usp_ScheduleOverride_GetById
            @EmployeeId, @OverrideId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 5. CANCEL SCHEDULE OVERRIDE
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Cancel
    @EmployeeId INT,
    @OverrideId BIGINT,
    @ExpectedRowVersion BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LockResult INT;

        DECLARE @LockResource NVARCHAR(255) =
            CONCAT(N'EMS.ScheduleOverride.', @EmployeeId);

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @LockResult < 0
            THROW 51304,
                'Could not obtain schedule lock.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.EmployeeScheduleOverrides
                WITH (UPDLOCK, HOLDLOCK)
            WHERE EmployeeId = @EmployeeId
              AND OverrideId = @OverrideId
        )
            THROW 51302, 'Override not found.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.EmployeeScheduleOverrides
            WHERE EmployeeId = @EmployeeId
              AND OverrideId = @OverrideId
              AND RecordStatus = 'ACTIVE'
        )
            THROW 51306, 'Override already cancelled.', 1;

        UPDATE dbo.EmployeeScheduleOverrides
        SET
            RecordStatus = 'CANCELLED',
            UpdatedAt = SYSUTCDATETIME()
        WHERE EmployeeId = @EmployeeId
          AND OverrideId = @OverrideId
          AND RowVersion = @ExpectedRowVersion;

        IF @@ROWCOUNT <> 1
            THROW 51305, 'Override changed concurrently.', 1;

        COMMIT TRANSACTION;

        EXEC dbo.usp_ScheduleOverride_GetById
            @EmployeeId, @OverrideId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 6. RESOLVE EMPLOYEE SCHEDULE
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeSchedule_Resolve
    @EmployeeId INT,
    @WorkDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Employees
        WHERE EmployeeId = @EmployeeId
    )
        THROW 51301, 'Employee not found.', 1;

    DECLARE
        @OverrideId BIGINT,
        @Kind VARCHAR(12),
        @Start DATETIMEOFFSET(0),
        @End DATETIMEOFFSET(0),
        @Break INT,
        @GraceIn INT,
        @GraceOut INT,
        @Required INT;

    SELECT
        @OverrideId = OverrideId,
        @Kind = OverrideType,
        @Start = StartAt,
        @End = EndAt,
        @Break = BreakMinutes,
        @GraceIn = GraceInMinutes,
        @GraceOut = GraceOutMinutes,
        @Required = RequiredMinutes
    FROM dbo.EmployeeScheduleOverrides
    WHERE EmployeeId = @EmployeeId
      AND WorkDate = @WorkDate
      AND RecordStatus = 'ACTIVE';

    -- Override takes priority over regular shift.
    IF @OverrideId IS NOT NULL
    BEGIN
        SELECT
            @EmployeeId AS EmployeeId,
            @WorkDate AS WorkDate,
            CAST('OVERRIDE' AS VARCHAR(12))
                AS ScheduleSource,
            @OverrideId AS OverrideId,
            CAST(NULL AS INT) AS ShiftId,
            @Kind AS OverrideType,
            CAST(
                IIF(@Kind = 'WORK', 1, 0)
                AS BIT
            ) AS IsScheduled,
            @Start AS ScheduledStartAt,
            @End AS ScheduledEndAt,
            @Break AS BreakMinutes,
            @GraceIn AS GraceInMinutes,
            @GraceOut AS GraceOutMinutes,
            CASE
                WHEN @Kind = 'WORK'
                THEN COALESCE(
                    @Required,
                    DATEDIFF(MINUTE, @Start, @End) - @Break
                )
                ELSE NULL
            END AS RequiredMinutes;

        RETURN;
    END;

    -- Find employee's weekly schedule.
    DECLARE @ScheduleId INT = (
        SELECT TOP (1) ScheduleId
        FROM dbo.EmployeeWorkScheduleAssignments
        WHERE EmployeeId = @EmployeeId
          AND EffectiveFrom <= @WorkDate
          AND (
              EffectiveTo IS NULL
              OR EffectiveTo >= @WorkDate
          )
        ORDER BY EffectiveFrom DESC
    );

    -- Fall back to organization weekly schedule.
    IF @ScheduleId IS NULL
        SELECT @ScheduleId = DefaultScheduleId
        FROM dbo.ScheduleSettings
        WHERE SettingsId = 1;

    -- Monday = 1 through Sunday = 7.
    DECLARE @Day INT =
        (
            (
                DATEDIFF(
                    DAY,
                    CONVERT(DATE, '19000101'),
                    @WorkDate
                ) % 7 + 7
            ) % 7
        ) + 1;

    DECLARE @Working BIT = (
        SELECT IsWorkingDay
        FROM dbo.WorkScheduleDays
        WHERE ScheduleId = @ScheduleId
          AND DayOfWeek = @Day
    );

    SET @Working = ISNULL(@Working, 0);

    -- Find effective regular shift.
    DECLARE @ShiftId INT = (
        SELECT TOP (1) ShiftId
        FROM dbo.EmployeeShiftAssignments
        WHERE EmployeeId = @EmployeeId
          AND EffectiveFrom <= @WorkDate
          AND (
              EffectiveTo IS NULL
              OR EffectiveTo >= @WorkDate
          )
        ORDER BY EffectiveFrom DESC
    );

    DECLARE
        @ShiftStart TIME,
        @ShiftEnd TIME,
        @ShiftBreak INT,
        @GI INT,
        @GO INT,
        @RM INT;

    SELECT
        @ShiftStart = StartTime,
        @ShiftEnd = EndTime,
        @ShiftBreak = BreakMinutes
    FROM dbo.Shifts
    WHERE ShiftId = @ShiftId;

    SELECT
        @GI = GraceInMinutes,
        @GO = GraceOutMinutes,
        @RM = RequiredMinutes
    FROM dbo.ShiftAttendancePolicies
    WHERE ShiftId = @ShiftId;

    DECLARE @Scheduled BIT =
        IIF(
            @Working = 1 AND @ShiftId IS NOT NULL,
            1, 0
        );

    DECLARE
        @StartLocal DATETIMEOFFSET(0) = NULL,
        @EndLocal DATETIMEOFFSET(0) = NULL;

    -- Current deployment assumption: UTC+05:00.
    IF @Scheduled = 1
    BEGIN
        SET @StartLocal = TODATETIMEOFFSET(
            DATEADD(
                SECOND,
                DATEDIFF(
                    SECOND,
                    CONVERT(TIME, '00:00'),
                    @ShiftStart
                ),
                CAST(@WorkDate AS DATETIME2(0))
            ),
            '+05:00'
        );

        SET @EndLocal = TODATETIMEOFFSET(
            DATEADD(
                DAY,
                IIF(@ShiftEnd <= @ShiftStart, 1, 0),
                DATEADD(
                    SECOND,
                    DATEDIFF(
                        SECOND,
                        CONVERT(TIME, '00:00'),
                        @ShiftEnd
                    ),
                    CAST(@WorkDate AS DATETIME2(0))
                )
            ),
            '+05:00'
        );
    END;

    SELECT
        @EmployeeId AS EmployeeId,
        @WorkDate AS WorkDate,
        CAST('REGULAR' AS VARCHAR(12))
            AS ScheduleSource,
        CAST(NULL AS BIGINT) AS OverrideId,
        @ShiftId AS ShiftId,
        CAST(NULL AS VARCHAR(12))
            AS OverrideType,
        @Scheduled AS IsScheduled,
        @StartLocal AS ScheduledStartAt,
        @EndLocal AS ScheduledEndAt,
        IIF(@Scheduled = 1, @ShiftBreak, NULL)
            AS BreakMinutes,
        IIF(@Scheduled = 1, ISNULL(@GI, 0), NULL)
            AS GraceInMinutes,
        IIF(@Scheduled = 1, ISNULL(@GO, 0), NULL)
            AS GraceOutMinutes,
        IIF(
            @Scheduled = 1,
            COALESCE(
                @RM,
                DATEDIFF(
                    MINUTE,
                    @StartLocal,
                    @EndLocal
                ) - @ShiftBreak
            ),
            NULL
        ) AS RequiredMinutes;
END;
GO
