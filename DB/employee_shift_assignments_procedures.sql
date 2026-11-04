USE EMS;
GO
/* Employee shift assignment history. Dates inclusive. Application time zone determines 'today'.
   Changes start on an effective date, trim preceding interval and stop before any later assignment.
   The filtered unique index on EffectiveTo IS NULL is preserved.
   No changes to existing tables. Assumes EmployeeShiftAssignments(AssignmentId,EmployeeId,ShiftId,EffectiveFrom,EffectiveTo).
*/
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeShift_List
    @EmployeeId INT
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId)
    THROW 51201, 'Employee not found.', 1;
 SELECT a.AssignmentId, a.EmployeeId, a.ShiftId, s.Name AS ShiftName,
        s.StartTime, s.EndTime, s.BreakMinutes, a.EffectiveFrom, a.EffectiveTo
 FROM dbo.EmployeeShiftAssignments a
 JOIN dbo.Shifts s ON s.ShiftId=a.ShiftId
 WHERE a.EmployeeId=@EmployeeId
 ORDER BY a.EffectiveFrom DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeShift_GetEffective
    @EmployeeId INT, @WorkDate DATE
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId)
    THROW 51201, 'Employee not found.', 1;
 SELECT a.AssignmentId, a.EmployeeId, a.ShiftId, s.Name AS ShiftName,
        s.StartTime, s.EndTime, s.BreakMinutes, a.EffectiveFrom, a.EffectiveTo
 FROM dbo.EmployeeShiftAssignments a
 JOIN dbo.Shifts s ON s.ShiftId=a.ShiftId
 WHERE a.EmployeeId=@EmployeeId AND a.EffectiveFrom<=@WorkDate
   AND (a.EffectiveTo IS NULL OR a.EffectiveTo>=@WorkDate);
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeShift_Assign
 @EmployeeId INT,
 @ShiftId INT,
 @EffectiveFrom DATE
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @EffectiveFrom IS NULL THROW 51202, 'EffectiveFrom is required.', 1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @lockResult INT;
  DECLARE @lockResource NVARCHAR(255) = CONCAT('EMS.EmployeeShift.',@EmployeeId);
  EXEC @lockResult = sys.sp_getapplock @Resource=@lockResource,
       @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
  IF @lockResult < 0 THROW 51210, 'Could not obtain employee shift assignment lock.', 1;
  IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId AND EmploymentStatus <> 'TERMINATED')
      THROW 51201, 'Employee not found or terminated.', 1;
  IF NOT EXISTS (SELECT 1 FROM dbo.Shifts WHERE ShiftId=@ShiftId AND IsActive=1)
      THROW 51203, 'Shift not found or inactive.', 1;
  IF EXISTS (SELECT 1 FROM dbo.EmployeeShiftAssignments WITH (UPDLOCK,HOLDLOCK)
             WHERE EmployeeId=@EmployeeId AND EffectiveFrom=@EffectiveFrom)
      THROW 51204, 'An assignment already begins on this date.', 1;
  DECLARE @nextStart DATE = (
      SELECT MIN(EffectiveFrom) FROM dbo.EmployeeShiftAssignments WITH (UPDLOCK,HOLDLOCK)
      WHERE EmployeeId=@EmployeeId AND EffectiveFrom > @EffectiveFrom);
  -- Shorten the interval covering the requested date, if any; no update of future rows.
  UPDATE dbo.EmployeeShiftAssignments
    SET EffectiveTo=DATEADD(DAY,-1,@EffectiveFrom)
    WHERE EmployeeId=@EmployeeId AND EffectiveFrom < @EffectiveFrom
       AND (EffectiveTo IS NULL OR EffectiveTo>=@EffectiveFrom);
  DECLARE @endDate DATE = CASE WHEN @nextStart IS NULL THEN NULL ELSE DATEADD(DAY,-1,@nextStart) END;
  INSERT dbo.EmployeeShiftAssignments(EmployeeId,ShiftId,EffectiveFrom,EffectiveTo)
  VALUES (@EmployeeId,@ShiftId,@EffectiveFrom,@endDate);
  COMMIT;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK;
  THROW;
 END CATCH;
 EXEC dbo.usp_EmployeeShift_GetEffective @EmployeeId=@EmployeeId,@WorkDate=@EffectiveFrom;
END;
GO
/* Initialize default shift for an existing employee with NO assignment history.
   Useful for one-off backfill; new employees are handled atomically by optional trigger below. */
CREATE OR ALTER PROCEDURE dbo.usp_EmployeeShift_InitializeDefault
 @EmployeeId INT
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @lockResult INT;
  DECLARE @lockResource NVARCHAR(255) = CONCAT('EMS.EmployeeShift.',@EmployeeId);
  EXEC @lockResult=sys.sp_getapplock @Resource=@lockResource,
    @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lockResult<0 THROW 51210,'Could not obtain employee shift assignment lock.',1;
  DECLARE @hire DATE=(SELECT HireDate FROM dbo.Employees WHERE EmployeeId=@EmployeeId);
  IF @hire IS NULL THROW 51201,'Employee not found.',1;
  DECLARE @shift INT=(SELECT DefaultShiftId FROM dbo.ScheduleSettings WHERE SettingsId=1);
  IF @shift IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Shifts WHERE ShiftId=@shift AND IsActive=1)
    THROW 51205,'Configure an active default shift first.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.EmployeeShiftAssignments WHERE EmployeeId=@EmployeeId)
   INSERT dbo.EmployeeShiftAssignments(EmployeeId,ShiftId,EffectiveFrom)
   VALUES (@EmployeeId,@shift,@hire);
  COMMIT;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH;
 EXEC dbo.usp_EmployeeShift_List @EmployeeId=@EmployeeId;
END;
GO
/* OPT-IN: Run only when ready for every new Employees insert to REQUIRE an active default shift.
   SQL Server trigger runs inside the employee INSERT transaction, even when EF Core creates the employee.
   It handles multi-row INSERT. An unset default shift blocks creation (intentional for guaranteed invariant).
*/
 CREATE OR ALTER TRIGGER dbo.trg_Employees_DefaultShift
 ON dbo.Employees AFTER INSERT
 AS
 BEGIN
  SET NOCOUNT ON;
  DECLARE @shift INT=(SELECT DefaultShiftId FROM dbo.ScheduleSettings WHERE SettingsId=1);
  IF @shift IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Shifts WHERE ShiftId=@shift AND IsActive=1)
      THROW 51205,'Configure an active default shift before creating employees.',1;
  INSERT dbo.EmployeeShiftAssignments(EmployeeId,ShiftId,EffectiveFrom,EffectiveTo)
  SELECT i.EmployeeId,@shift,i.HireDate,NULL
  FROM inserted i
  WHERE NOT EXISTS(SELECT 1 FROM dbo.EmployeeShiftAssignments a WHERE a.EmployeeId=i.EmployeeId);
 END;
 GO
