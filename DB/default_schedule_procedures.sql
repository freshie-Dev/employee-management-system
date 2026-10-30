USE EMS;
GO

/* Read configured defaults and their display names. */
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleSettings_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ss.SettingsId, ss.DefaultShiftId, sh.Name AS DefaultShiftName,
           ss.DefaultScheduleId, ws.Name AS DefaultScheduleName,
           ss.DefaultHolidayCalendarId, ss.UpdatedAt
    FROM dbo.ScheduleSettings AS ss
    LEFT JOIN dbo.Shifts AS sh ON sh.ShiftId = ss.DefaultShiftId
    LEFT JOIN dbo.WorkSchedules AS ws ON ws.ScheduleId = ss.DefaultScheduleId
    WHERE ss.SettingsId = 1;
END;
GO

/* Atomically update organization-wide defaults; nullable holiday calendar is permitted.
   All three values are required in the request, even when a value is SQL NULL. */
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleSettings_Update
    @DefaultShiftId INT,
    @DefaultScheduleId INT,
    @DefaultHolidayCalendarId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Shifts WHERE ShiftId = @DefaultShiftId AND IsActive = 1)
            THROW 51101, 'Default shift must exist and be active.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.WorkSchedules WHERE ScheduleId = @DefaultScheduleId AND IsActive = 1)
            THROW 51102, 'Default weekly schedule must exist and be active.', 1;

        IF @DefaultHolidayCalendarId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @DefaultHolidayCalendarId)
            THROW 51103, 'Holiday calendar does not exist.', 1;

        UPDATE dbo.ScheduleSettings WITH (UPDLOCK, HOLDLOCK)
        SET DefaultShiftId = @DefaultShiftId,
            DefaultScheduleId = @DefaultScheduleId,
            DefaultHolidayCalendarId = @DefaultHolidayCalendarId,
            UpdatedAt = SYSUTCDATETIME()
        WHERE SettingsId = 1;

        IF @@ROWCOUNT = 0
            THROW 51104, 'Singleton ScheduleSettings row is missing.', 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    EXEC dbo.usp_ScheduleSettings_Get;
END;
GO

/* List weekly schedule templates and their seven weekday flags. */
CREATE OR ALTER PROCEDURE dbo.usp_WorkSchedule_List
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ws.ScheduleId, ws.Name, ws.IsActive, ws.CreatedAt,
           d.DayOfWeek, d.IsWorkingDay
    FROM dbo.WorkSchedules AS ws
    LEFT JOIN dbo.WorkScheduleDays AS d ON d.ScheduleId = ws.ScheduleId
    WHERE @IncludeInactive = 1 OR ws.IsActive = 1
    ORDER BY ws.Name, d.DayOfWeek;
END;
GO

/* Create an entire seven-day template in one transaction.
   @WorkingDaysJson examples: '[1,2,3,4,5]' (Mon-Fri), '[1,2,3,4,5,6]' (Mon-Sat).
   JSON array may be empty to define a fully nonworking template. */
CREATE OR ALTER PROCEDURE dbo.usp_WorkSchedule_Create
    @Name NVARCHAR(100),
    @WorkingDaysJson NVARCHAR(MAX),
    @ScheduleId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL
        THROW 51105, 'Schedule name is required.', 1;

    IF ISJSON(@WorkingDaysJson) <> 1 OR LEFT(LTRIM(@WorkingDaysJson), 1) <> '['
        THROW 51106, 'Working days must be a JSON array.', 1;

    DECLARE @Days TABLE (DayOfWeek TINYINT PRIMARY KEY);
    DECLARE @JsonDays TABLE (JsonKey INT, JsonValue NVARCHAR(100), JsonType INT);

    INSERT @JsonDays(JsonKey, JsonValue, JsonType)
    SELECT TRY_CONVERT(INT, [key]), [value], [type]
    FROM OPENJSON(@WorkingDaysJson);

    IF EXISTS (
        SELECT 1 FROM @JsonDays
        WHERE JsonType <> 2 OR TRY_CONVERT(TINYINT, JsonValue) NOT BETWEEN 1 AND 7
           OR TRY_CONVERT(INT, JsonValue) IS NULL
           OR TRY_CONVERT(INT, JsonValue) <> TRY_CONVERT(DECIMAL(15,5), JsonValue)
    ) OR (SELECT COUNT(*) FROM @JsonDays) <> (SELECT COUNT(DISTINCT JsonValue) FROM @JsonDays)
        THROW 51107, 'Working days must be unique integers from 1 (Monday) to 7 (Sunday).', 1;

    INSERT @Days(DayOfWeek)
    SELECT CONVERT(TINYINT, JsonValue) FROM @JsonDays;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.WorkSchedules WITH (UPDLOCK, HOLDLOCK) WHERE Name = LTRIM(RTRIM(@Name)))
            THROW 51108, 'Weekly schedule name already exists.', 1;

        INSERT dbo.WorkSchedules(Name) VALUES (LTRIM(RTRIM(@Name)));
        SET @ScheduleId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.WorkScheduleDays(ScheduleId, DayOfWeek, IsWorkingDay)
        SELECT @ScheduleId, d.DayOfWeek,
               CONVERT(BIT, CASE WHEN wd.DayOfWeek IS NULL THEN 0 ELSE 1 END)
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7)) AS d(DayOfWeek)
        LEFT JOIN @Days AS wd ON wd.DayOfWeek = d.DayOfWeek;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT ws.ScheduleId, ws.Name, ws.IsActive, ws.CreatedAt,
           d.DayOfWeek, d.IsWorkingDay
    FROM dbo.WorkSchedules ws
    JOIN dbo.WorkScheduleDays d ON d.ScheduleId = ws.ScheduleId
    WHERE ws.ScheduleId = @ScheduleId
    ORDER BY d.DayOfWeek;
END;
GO

/* Smoke tests (safe reads only):
EXEC dbo.usp_ScheduleSettings_Get;
EXEC dbo.usp_WorkSchedule_List @IncludeInactive = 1;

-- Mutating examples, run only when desired:
DECLARE @ScheduleId INT;
EXEC dbo.usp_WorkSchedule_Create
    @Name = N'Monday-Friday', @WorkingDaysJson = N'[1,2,3,4,5]',
    @ScheduleId = @ScheduleId OUTPUT;
SELECT @ScheduleId AS CreatedScheduleId;

EXEC dbo.usp_ScheduleSettings_Update
    @DefaultShiftId = 1,
    @DefaultScheduleId = @ScheduleId,
    @DefaultHolidayCalendarId = NULL;
*/
