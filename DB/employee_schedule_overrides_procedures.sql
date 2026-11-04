USE EMS;
GO
-- Requires dbo.EmployeeScheduleOverrides from EMS_Shift_Attendance_Extensions.sql.
-- Inputs use DATETIMEOFFSET with explicit offset. WorkDate is the local date at StartAt.
-- Error codes: 51301 missing employee, 51302 not found, 51303 invalid time,
-- 51304 conflict, 51305 concurrency, 51306 invalid state, 51307 denied.
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_ListByEmployee
 @EmployeeId INT,
 @FromDate DATE = NULL,
 @ToDate DATE = NULL,
 @IncludeCancelled BIT = 0
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId)
  THROW 51301,'Employee not found.',1;
 SELECT OverrideId,EmployeeId,WorkDate,OverrideType,StartAt,EndAt,
        BreakMinutes,GraceInMinutes,GraceOutMinutes,RequiredMinutes,Reason,
        AssignedByUserId,RecordStatus,CreatedAt,UpdatedAt,RowVersion
 FROM dbo.EmployeeScheduleOverrides
 WHERE EmployeeId=@EmployeeId
 AND (@FromDate IS NULL OR WorkDate>=@FromDate)
 AND (@ToDate IS NULL OR WorkDate<=@ToDate)
 AND (@IncludeCancelled=1 OR RecordStatus='ACTIVE')
 ORDER BY WorkDate DESC,OverrideId DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_GetById
 @EmployeeId INT,@OverrideId BIGINT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT OverrideId,EmployeeId,WorkDate,OverrideType,StartAt,EndAt,
        BreakMinutes,GraceInMinutes,GraceOutMinutes,RequiredMinutes,Reason,
        AssignedByUserId,RecordStatus,CreatedAt,UpdatedAt,RowVersion
 FROM dbo.EmployeeScheduleOverrides
 WHERE EmployeeId=@EmployeeId AND OverrideId=@OverrideId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Create
 @EmployeeId INT,@WorkDate DATE,@OverrideType VARCHAR(12),
 @StartAt DATETIMEOFFSET(0)=NULL,@EndAt DATETIMEOFFSET(0)=NULL,
 @BreakMinutes INT=0,@GraceInMinutes INT=0,@GraceOutMinutes INT=0,
 @RequiredMinutes INT=NULL,@Reason NVARCHAR(500)=NULL,@AssignedByUserId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @WorkDate IS NULL OR @OverrideType NOT IN ('WORK','DAY_OFF') OR @OverrideType IS NULL
    THROW 51303,'Invalid date or override type.',1;
 IF @BreakMinutes NOT BETWEEN 0 AND 1440 OR @GraceInMinutes NOT BETWEEN 0 AND 240
    OR @GraceOutMinutes NOT BETWEEN 0 AND 240 OR (@RequiredMinutes IS NOT NULL AND @RequiredMinutes NOT BETWEEN 0 AND 1440)
    THROW 51303,'Invalid break/grace/required minutes.',1;
 IF (@OverrideType='DAY_OFF' AND (@StartAt IS NOT NULL OR @EndAt IS NOT NULL OR @BreakMinutes<>0 OR @RequiredMinutes IS NOT NULL))
    OR (@OverrideType='WORK' AND (@StartAt IS NULL OR @EndAt IS NULL))
    THROW 51303,'DAY_OFF must have no times; WORK requires start and end.',1;
 IF @OverrideType='WORK'
 BEGIN
  DECLARE @span INT=DATEDIFF(MINUTE,@StartAt,@EndAt);
  IF @span NOT BETWEEN 1 AND 1440 OR @BreakMinutes>=@span
      OR (@RequiredMinutes IS NOT NULL AND @RequiredMinutes>@span-@BreakMinutes)
      OR CONVERT(date,@StartAt)<>@WorkDate
   THROW 51303,'Invalid work interval, duration or starting work date.',1;
 END;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @lockResult INT;
  EXEC @lockResult=sys.sp_getapplock @Resource=CONCAT('EMS.ScheduleOverride.',@EmployeeId),
       @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lockResult<0 THROW 51304,'Could not obtain schedule lock.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId)
     THROW 51301,'Employee not found.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.UserAccounts WHERE UserId=@AssignedByUserId)
     THROW 51307,'Assigning user not found.',1;
  IF EXISTS(SELECT 1 FROM dbo.EmployeeScheduleOverrides WITH(UPDLOCK,HOLDLOCK)
      WHERE EmployeeId=@EmployeeId AND WorkDate=@WorkDate AND RecordStatus='ACTIVE')
     THROW 51304,'An active override already exists for this work date.',1;
  IF @OverrideType='WORK' AND EXISTS(
    SELECT 1 FROM dbo.EmployeeScheduleOverrides WITH(UPDLOCK,HOLDLOCK)
    WHERE EmployeeId=@EmployeeId AND RecordStatus='ACTIVE' AND OverrideType='WORK'
      AND StartAt<@EndAt AND EndAt>@StartAt)
     THROW 51304,'Work interval overlaps an existing active override.',1;
  INSERT dbo.EmployeeScheduleOverrides
      (EmployeeId,WorkDate,OverrideType,StartAt,EndAt,BreakMinutes,
       GraceInMinutes,GraceOutMinutes,RequiredMinutes,Reason,AssignedByUserId)
  VALUES(@EmployeeId,@WorkDate,@OverrideType,@StartAt,@EndAt,@BreakMinutes,
         @GraceInMinutes,@GraceOutMinutes,@RequiredMinutes,@Reason,@AssignedByUserId);
  DECLARE @id BIGINT=SCOPE_IDENTITY();
  COMMIT;
  EXEC dbo.usp_ScheduleOverride_GetById @EmployeeId,@id;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK; THROW;
 END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Update
 @EmployeeId INT,@OverrideId BIGINT,@ExpectedRowVersion BINARY(8),
 @OverrideType VARCHAR(12),@StartAt DATETIMEOFFSET(0)=NULL,@EndAt DATETIMEOFFSET(0)=NULL,
 @BreakMinutes INT=0,@GraceInMinutes INT=0,@GraceOutMinutes INT=0,
 @RequiredMinutes INT=NULL,@Reason NVARCHAR(500)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @WorkDate DATE;
 SELECT @WorkDate=WorkDate FROM dbo.EmployeeScheduleOverrides
 WHERE EmployeeId=@EmployeeId AND OverrideId=@OverrideId;
 IF @WorkDate IS NULL THROW 51302,'Override not found.',1;
 IF @OverrideType NOT IN ('WORK','DAY_OFF') OR @OverrideType IS NULL
    OR @BreakMinutes NOT BETWEEN 0 AND 1440 OR @GraceInMinutes NOT BETWEEN 0 AND 240
    OR @GraceOutMinutes NOT BETWEEN 0 AND 240 OR (@RequiredMinutes IS NOT NULL AND @RequiredMinutes NOT BETWEEN 0 AND 1440)
    THROW 51303,'Invalid override values.',1;
 IF (@OverrideType='DAY_OFF' AND (@StartAt IS NOT NULL OR @EndAt IS NOT NULL OR @BreakMinutes<>0 OR @RequiredMinutes IS NOT NULL))
    OR (@OverrideType='WORK' AND (@StartAt IS NULL OR @EndAt IS NULL))
    THROW 51303,'DAY_OFF must have no times; WORK requires start and end.',1;
 IF @OverrideType='WORK'
 BEGIN
  DECLARE @span INT=DATEDIFF(MINUTE,@StartAt,@EndAt);
  IF @span NOT BETWEEN 1 AND 1440 OR @BreakMinutes>=@span
    OR (@RequiredMinutes IS NOT NULL AND @RequiredMinutes>@span-@BreakMinutes)
    OR CONVERT(date,@StartAt)<>@WorkDate
   THROW 51303,'Invalid work interval or date.',1;
 END;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @lockResult INT;
 EXEC @lockResult=sys.sp_getapplock @Resource=CONCAT('EMS.ScheduleOverride.',@EmployeeId),
    @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lockResult<0 THROW 51304,'Could not obtain schedule lock.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.EmployeeScheduleOverrides WITH(UPDLOCK,HOLDLOCK)
    WHERE OverrideId=@OverrideId AND EmployeeId=@EmployeeId AND RecordStatus='ACTIVE')
    THROW 51306,'Override is missing or cancelled.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.EmployeeScheduleOverrides
    WHERE OverrideId=@OverrideId AND EmployeeId=@EmployeeId AND RowVersion=@ExpectedRowVersion)
    THROW 51305,'Override changed since it was retrieved.',1;
 IF @OverrideType='WORK' AND EXISTS(
    SELECT 1 FROM dbo.EmployeeScheduleOverrides WITH(UPDLOCK,HOLDLOCK)
    WHERE EmployeeId=@EmployeeId AND OverrideId<>@OverrideId
      AND RecordStatus='ACTIVE' AND OverrideType='WORK' AND StartAt<@EndAt AND EndAt>@StartAt)
    THROW 51304,'Work interval overlaps another override.',1;
 UPDATE dbo.EmployeeScheduleOverrides
 SET OverrideType=@OverrideType,StartAt=@StartAt,EndAt=@EndAt,
     BreakMinutes=@BreakMinutes,GraceInMinutes=@GraceInMinutes,
     GraceOutMinutes=@GraceOutMinutes,RequiredMinutes=@RequiredMinutes,
     Reason=@Reason,UpdatedAt=SYSUTCDATETIME()
 WHERE OverrideId=@OverrideId AND EmployeeId=@EmployeeId AND RowVersion=@ExpectedRowVersion;
 IF @@ROWCOUNT<>1 THROW 51305,'Override changed concurrently.',1;
 COMMIT;
 EXEC dbo.usp_ScheduleOverride_GetById @EmployeeId,@OverrideId;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK; THROW;
 END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_ScheduleOverride_Cancel
 @EmployeeId INT,@OverrideId BIGINT,@ExpectedRowVersion BINARY(8)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @lockResult INT;
 EXEC @lockResult=sys.sp_getapplock @Resource=CONCAT('EMS.ScheduleOverride.',@EmployeeId),
     @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lockResult<0 THROW 51304,'Could not obtain schedule lock.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.EmployeeScheduleOverrides WITH(UPDLOCK,HOLDLOCK)
   WHERE EmployeeId=@EmployeeId AND OverrideId=@OverrideId)
     THROW 51302,'Override not found.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.EmployeeScheduleOverrides
    WHERE EmployeeId=@EmployeeId AND OverrideId=@OverrideId AND RecordStatus='ACTIVE')
     THROW 51306,'Override already cancelled.',1;
 UPDATE dbo.EmployeeScheduleOverrides SET RecordStatus='CANCELLED',UpdatedAt=SYSUTCDATETIME()
 WHERE EmployeeId=@EmployeeId AND OverrideId=@OverrideId AND RowVersion=@ExpectedRowVersion;
 IF @@ROWCOUNT<>1 THROW 51305,'Override changed concurrently.',1;
 COMMIT;
 EXEC dbo.usp_ScheduleOverride_GetById @EmployeeId,@OverrideId;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK; THROW;
 END CATCH;
END;
GO
-- Resolves the effective override or base shift/week pattern. Does NOT yet evaluate holiday/leave.
-- Default regular shift is NOT automatically assigned on employee creation.
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeSchedule_Resolve
 @EmployeeId INT,@WorkDate DATE
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId)
   THROW 51301,'Employee not found.',1;
 DECLARE @overrideId BIGINT,@kind VARCHAR(12),@start DATETIMEOFFSET(0),@end DATETIMEOFFSET(0),
    @break INT,@graceIn INT,@graceOut INT,@required INT;
 SELECT @overrideId=OverrideId,@kind=OverrideType,@start=StartAt,@end=EndAt,
    @break=BreakMinutes,@graceIn=GraceInMinutes,@graceOut=GraceOutMinutes,@required=RequiredMinutes
 FROM dbo.EmployeeScheduleOverrides WHERE EmployeeId=@EmployeeId AND WorkDate=@WorkDate AND RecordStatus='ACTIVE';
 IF @overrideId IS NOT NULL
 BEGIN
  SELECT @EmployeeId AS EmployeeId,@WorkDate AS WorkDate,CAST('OVERRIDE' AS VARCHAR(12)) AS ScheduleSource,
   @overrideId AS OverrideId, CAST(NULL AS INT) AS ShiftId, @kind AS OverrideType,
   CAST(IIF(@kind='WORK',1,0) AS BIT) AS IsScheduled,
   @start AS ScheduledStartAt,@end AS ScheduledEndAt,@break AS BreakMinutes,
   @graceIn AS GraceInMinutes,@graceOut AS GraceOutMinutes,
   CASE WHEN @kind='WORK' THEN COALESCE(@required,DATEDIFF(MINUTE,@start,@end)-@break) ELSE NULL END AS RequiredMinutes;
  RETURN;
 END;
 DECLARE @scheduleId INT = (
   SELECT TOP(1) ScheduleId FROM dbo.EmployeeWorkScheduleAssignments
   WHERE EmployeeId=@EmployeeId AND EffectiveFrom<=@WorkDate
   AND (EffectiveTo IS NULL OR EffectiveTo>=@WorkDate) ORDER BY EffectiveFrom DESC);
 IF @scheduleId IS NULL SELECT @scheduleId=DefaultScheduleId FROM dbo.ScheduleSettings WHERE SettingsId=1;
 DECLARE @day INT=((DATEDIFF(DAY,CONVERT(date,'19000101'),@WorkDate)%7+7)%7)+1;
 DECLARE @working BIT=(SELECT IsWorkingDay FROM dbo.WorkScheduleDays WHERE ScheduleId=@scheduleId AND DayOfWeek=@day);
 SET @working=ISNULL(@working,0);
 DECLARE @shiftId INT=(SELECT TOP(1) ShiftId FROM dbo.EmployeeShiftAssignments
  WHERE EmployeeId=@EmployeeId AND EffectiveFrom<=@WorkDate
  AND (EffectiveTo IS NULL OR EffectiveTo>=@WorkDate) ORDER BY EffectiveFrom DESC);
 DECLARE @shiftStart TIME,@shiftEnd TIME,@shiftBreak INT,@gi INT,@go INT,@rm INT;
 SELECT @shiftStart=StartTime,@shiftEnd=EndTime,@shiftBreak=BreakMinutes
 FROM dbo.Shifts WHERE ShiftId=@shiftId;
 SELECT @gi=GraceInMinutes,@go=GraceOutMinutes,@rm=RequiredMinutes
 FROM dbo.ShiftAttendancePolicies WHERE ShiftId=@shiftId;
 DECLARE @scheduled BIT=IIF(@working=1 AND @shiftId IS NOT NULL,1,0);
 -- Offset +05:00 is a deployment assumption for Pakistan; change if organization supports other regions.
 DECLARE @startLocal DATETIMEOFFSET(0)=NULL,@endLocal DATETIMEOFFSET(0)=NULL;
 IF @scheduled=1
 BEGIN
  SET @startLocal=TODATETIMEOFFSET(DATEADD(SECOND,DATEDIFF(SECOND,CONVERT(time,'00:00'),@shiftStart),CAST(@WorkDate AS DATETIME2(0))),'+05:00');
  SET @endLocal=TODATETIMEOFFSET(DATEADD(DAY,IIF(@shiftEnd<=@shiftStart,1,0),
    DATEADD(SECOND,DATEDIFF(SECOND,CONVERT(time,'00:00'),@shiftEnd),CAST(@WorkDate AS DATETIME2(0)))),'+05:00');
 END;
 SELECT @EmployeeId AS EmployeeId,@WorkDate AS WorkDate,
  CAST('REGULAR' AS VARCHAR(12)) AS ScheduleSource,
  CAST(NULL AS BIGINT) AS OverrideId,@shiftId AS ShiftId,
  CAST(NULL AS VARCHAR(12)) AS OverrideType,@scheduled AS IsScheduled,
  @startLocal AS ScheduledStartAt,@endLocal AS ScheduledEndAt,
  IIF(@scheduled=1,@shiftBreak,NULL) AS BreakMinutes,
  IIF(@scheduled=1,ISNULL(@gi,0),NULL) AS GraceInMinutes,
  IIF(@scheduled=1,ISNULL(@go,0),NULL) AS GraceOutMinutes,
  IIF(@scheduled=1,COALESCE(@rm,DATEDIFF(MINUTE,@startLocal,@endLocal)-@shiftBreak),NULL) AS RequiredMinutes;
END;
GO
