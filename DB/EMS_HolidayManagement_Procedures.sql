/* EMS Holiday Calendar Management - DB first
   Based on existing dbo.HolidayCalendars and dbo.Holidays.
   No CREATE TABLE or schema alterations. SQL Server 2016 SP1+.
   One holiday per calendar/date (existing UQ_Holidays_Date).
   Business errors: 51401 calendar missing; 51402 holiday missing;
                    51403 invalid request; 51404 calendar inactive;
                    51405 default calendar cannot deactivate.
*/
USE EMS;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HolidayCalendar_List
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CalendarId, c.Name, c.IsActive,
           (SELECT COUNT(*) FROM dbo.Holidays h WHERE h.CalendarId = c.CalendarId) AS HolidayCount,
           CAST(CASE WHEN c.CalendarId = (SELECT DefaultHolidayCalendarId FROM dbo.ScheduleSettings WHERE SettingsId = 1)
                     THEN 1 ELSE 0 END AS BIT) AS IsDefault
    FROM dbo.HolidayCalendars c
    WHERE @IncludeInactive = 1 OR c.IsActive = 1
    ORDER BY c.Name;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HolidayCalendar_GetById
    @CalendarId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CalendarId, c.Name, c.IsActive,
           (SELECT COUNT(*) FROM dbo.Holidays h WHERE h.CalendarId = c.CalendarId) AS HolidayCount,
           CAST(CASE WHEN c.CalendarId = (SELECT DefaultHolidayCalendarId FROM dbo.ScheduleSettings WHERE SettingsId = 1)
                     THEN 1 ELSE 0 END AS BIT) AS IsDefault
    FROM dbo.HolidayCalendars c WHERE c.CalendarId = @CalendarId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HolidayCalendar_Create
    @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Name = NULLIF(LTRIM(RTRIM(@Name)), N'');
    IF @Name IS NULL THROW 51403, 'Calendar name is required.', 1;
    INSERT dbo.HolidayCalendars(Name, IsActive) VALUES (@Name, 1);
    DECLARE @Id INT = CONVERT(INT, SCOPE_IDENTITY());
    EXEC dbo.usp_HolidayCalendar_GetById @CalendarId = @Id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HolidayCalendar_Update
    @CalendarId INT, @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Name = NULLIF(LTRIM(RTRIM(@Name)), N'');
    IF @Name IS NULL THROW 51403, 'Calendar name is required.', 1;
    UPDATE dbo.HolidayCalendars SET Name = @Name WHERE CalendarId = @CalendarId;
    IF @@ROWCOUNT = 0 THROW 51401, 'Calendar not found.', 1;
    EXEC dbo.usp_HolidayCalendar_GetById @CalendarId = @CalendarId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_HolidayCalendar_SetStatus
    @CalendarId INT, @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId)
        THROW 51401, 'Calendar not found.', 1;
    IF @IsActive = 0 AND EXISTS (
        SELECT 1 FROM dbo.ScheduleSettings WHERE SettingsId = 1 AND DefaultHolidayCalendarId = @CalendarId)
        THROW 51405, 'Change the organization default holiday calendar before deactivating this one.', 1;
    UPDATE dbo.HolidayCalendars SET IsActive = @IsActive WHERE CalendarId = @CalendarId;
    EXEC dbo.usp_HolidayCalendar_GetById @CalendarId = @CalendarId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_List
    @CalendarId INT,
    @FromDate DATE = NULL,
    @ToDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId)
        THROW 51401, 'Calendar not found.', 1;
    IF @FromDate IS NOT NULL AND @ToDate IS NOT NULL AND @FromDate > @ToDate
        THROW 51403, 'FromDate must be on or before ToDate.', 1;
    SELECT h.HolidayId, h.CalendarId, h.HolidayDate, h.Name
    FROM dbo.Holidays h
    WHERE h.CalendarId = @CalendarId
      AND (@FromDate IS NULL OR h.HolidayDate >= @FromDate)
      AND (@ToDate IS NULL OR h.HolidayDate <= @ToDate)
    ORDER BY h.HolidayDate, h.HolidayId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_GetById
    @CalendarId INT, @HolidayId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT HolidayId, CalendarId, HolidayDate, Name
    FROM dbo.Holidays
    WHERE CalendarId = @CalendarId AND HolidayId = @HolidayId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_Create
    @CalendarId INT, @HolidayDate DATE, @Name NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Name = NULLIF(LTRIM(RTRIM(@Name)), N'');
    IF @HolidayDate IS NULL OR @Name IS NULL THROW 51403, 'Holiday date and name are required.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId)
        THROW 51401, 'Calendar not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId AND IsActive = 1)
        THROW 51404, 'Cannot add holidays to an inactive calendar.', 1;
    INSERT dbo.Holidays(CalendarId, HolidayDate, Name)
    VALUES (@CalendarId, @HolidayDate, @Name);
    DECLARE @Id INT = CONVERT(INT, SCOPE_IDENTITY());
    EXEC dbo.usp_Holiday_GetById @CalendarId = @CalendarId, @HolidayId = @Id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_Update
    @CalendarId INT, @HolidayId INT, @HolidayDate DATE, @Name NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Name = NULLIF(LTRIM(RTRIM(@Name)), N'');
    IF @HolidayDate IS NULL OR @Name IS NULL THROW 51403, 'Holiday date and name are required.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId AND IsActive = 1)
        THROW 51404, 'Calendar is missing or inactive.', 1;
    UPDATE dbo.Holidays SET HolidayDate = @HolidayDate, Name = @Name
    WHERE CalendarId = @CalendarId AND HolidayId = @HolidayId;
    IF @@ROWCOUNT = 0 THROW 51402, 'Holiday not found.', 1;
    EXEC dbo.usp_Holiday_GetById @CalendarId = @CalendarId, @HolidayId = @HolidayId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_Delete
    @CalendarId INT, @HolidayId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DELETE FROM dbo.Holidays WHERE CalendarId = @CalendarId AND HolidayId = @HolidayId;
    IF @@ROWCOUNT = 0 THROW 51402, 'Holiday not found.', 1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Holiday_CheckDate
    @CalendarId INT, @WorkDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.HolidayCalendars WHERE CalendarId = @CalendarId)
        THROW 51401, 'Calendar not found.', 1;
    SELECT @CalendarId AS CalendarId, @WorkDate AS WorkDate,
           CAST(CASE WHEN c.IsActive = 1 AND h.HolidayId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsHoliday,
           h.HolidayId, h.Name AS HolidayName
    FROM dbo.HolidayCalendars c
    LEFT JOIN dbo.Holidays h ON h.CalendarId = c.CalendarId AND h.HolidayDate = @WorkDate
    WHERE c.CalendarId = @CalendarId;
END;
GO
