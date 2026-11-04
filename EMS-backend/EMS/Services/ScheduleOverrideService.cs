using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.ScheduleOverrides;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;
namespace EMS.Services;
public sealed class ScheduleOverrideService(IStoredProcedureExecutor executor) : IScheduleOverrideService
{
 private static void Int(SqlParameterCollection p,string n,int v)=>p.Add(n,SqlDbType.Int).Value=v;
 private static void Big(SqlParameterCollection p,string n,long v)=>p.Add(n,SqlDbType.BigInt).Value=v;
 private static void Date(SqlParameterCollection p,string n,DateOnly? v)=>p.Add(n,SqlDbType.Date).Value=v.HasValue?v.Value.ToDateTime(TimeOnly.MinValue):DBNull.Value;
 private static void OptDate(SqlParameterCollection p,string n,DateTimeOffset? v)=>p.Add(n,SqlDbType.DateTimeOffset).Value=v.HasValue?v.Value:DBNull.Value;
 private static void Policy(SqlParameterCollection p,string type,DateTimeOffset? start,DateTimeOffset? end,int breaks,int gi,int go,int? req,string? reason)
 {
  p.Add("@OverrideType",SqlDbType.VarChar,12).Value=type;
  OptDate(p,"@StartAt",start);OptDate(p,"@EndAt",end);
  Int(p,"@BreakMinutes",breaks);Int(p,"@GraceInMinutes",gi);Int(p,"@GraceOutMinutes",go);
  p.Add("@RequiredMinutes",SqlDbType.Int).Value=req.HasValue?req.Value:DBNull.Value;
  p.Add("@Reason",SqlDbType.NVarChar,500).Value=reason is null?DBNull.Value:reason;
 }
 private static byte[] ParseVersion(string input)
 {
  try { var bytes=Convert.FromBase64String(input); if(bytes.Length==8)return bytes; }
  catch(FormatException) { }
  throw new ArgumentException("rowVersion must be a base64-encoded 8-byte SQL rowversion.");
 }
 private static ScheduleOverrideDto Map(SqlDataReader r)
 {
  T? Nullable<T>(string name,Func<int,T> get) where T:struct { int i=r.GetOrdinal(name);return r.IsDBNull(i)?null:get(i); }
  string? Str(string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:r.GetString(i);}
  return new(r.GetInt64(r.GetOrdinal("OverrideId")),r.GetInt32(r.GetOrdinal("EmployeeId")),
    DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("WorkDate"))),r.GetString(r.GetOrdinal("OverrideType")),
    Nullable("StartAt",r.GetDateTimeOffset),Nullable("EndAt",r.GetDateTimeOffset),
    r.GetInt32(r.GetOrdinal("BreakMinutes")),r.GetInt32(r.GetOrdinal("GraceInMinutes")),r.GetInt32(r.GetOrdinal("GraceOutMinutes")),
    Nullable("RequiredMinutes",r.GetInt32),Str("Reason"),r.GetInt32(r.GetOrdinal("AssignedByUserId")),
    r.GetString(r.GetOrdinal("RecordStatus")),r.GetDateTime(r.GetOrdinal("CreatedAt")),r.GetDateTime(r.GetOrdinal("UpdatedAt")),
    Convert.ToBase64String((byte[])r["RowVersion"]));
 }
 private static ResolvedEmployeeScheduleDto MapResolved(SqlDataReader r)
 {
  T? N<T>(string n,Func<int,T> f) where T:struct {int i=r.GetOrdinal(n);return r.IsDBNull(i)?null:f(i);}
  return new(r.GetInt32(r.GetOrdinal("EmployeeId")),DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("WorkDate"))),
   r.GetString(r.GetOrdinal("ScheduleSource")),N("OverrideId",r.GetInt64),N("ShiftId",r.GetInt32),
   (r.IsDBNull(r.GetOrdinal("OverrideType"))?null:r.GetString(r.GetOrdinal("OverrideType"))),r.GetBoolean(r.GetOrdinal("IsScheduled")),
   N("ScheduledStartAt",r.GetDateTimeOffset),N("ScheduledEndAt",r.GetDateTimeOffset),
   N("BreakMinutes",r.GetInt32),N("GraceInMinutes",r.GetInt32),N("GraceOutMinutes",r.GetInt32),N("RequiredMinutes",r.GetInt32));
 }
 public async Task<IReadOnlyList<ScheduleOverrideDto>> ListAsync(int employeeId,DateOnly? from,DateOnly? to,bool includeCancelled,CancellationToken ct)=>
  await executor.QueryAsync("dbo.usp_ScheduleOverride_ListByEmployee",p=>{
   Int(p,"@EmployeeId",employeeId);Date(p,"@FromDate",from);Date(p,"@ToDate",to);
   p.Add("@IncludeCancelled",SqlDbType.Bit).Value=includeCancelled;},Map,ct);
 public Task<ScheduleOverrideDto?> GetAsync(int employeeId,long overrideId,CancellationToken ct)=>
  executor.QuerySingleAsync("dbo.usp_ScheduleOverride_GetById",p=>{Int(p,"@EmployeeId",employeeId);Big(p,"@OverrideId",overrideId);},Map,ct);
 public async Task<ScheduleOverrideDto> CreateAsync(int employeeId,int actorUserId,CreateScheduleOverrideDto dto,CancellationToken ct)
 {
  var row=await executor.QuerySingleAsync("dbo.usp_ScheduleOverride_Create",p=>{
   Int(p,"@EmployeeId",employeeId);Date(p,"@WorkDate",dto.WorkDate);
   Policy(p,dto.OverrideType,dto.StartAt,dto.EndAt,dto.BreakMinutes,dto.GraceInMinutes,dto.GraceOutMinutes,dto.RequiredMinutes,dto.Reason);
   Int(p,"@AssignedByUserId",actorUserId);},Map,ct);
  return row??throw new InvalidOperationException("Create procedure returned no row.");
 }
 public async Task<ScheduleOverrideDto> UpdateAsync(int employeeId,long overrideId,UpdateScheduleOverrideDto dto,CancellationToken ct)
 {
  var row=await executor.QuerySingleAsync("dbo.usp_ScheduleOverride_Update",p=>{
   Int(p,"@EmployeeId",employeeId);Big(p,"@OverrideId",overrideId);
   p.Add("@ExpectedRowVersion",SqlDbType.Binary,8).Value=ParseVersion(dto.RowVersion);
   Policy(p,dto.OverrideType,dto.StartAt,dto.EndAt,dto.BreakMinutes,dto.GraceInMinutes,dto.GraceOutMinutes,dto.RequiredMinutes,dto.Reason);},Map,ct);
  return row??throw new InvalidOperationException("Update procedure returned no row.");
 }
 public async Task<ScheduleOverrideDto> CancelAsync(int employeeId,long overrideId,CancelScheduleOverrideDto dto,CancellationToken ct)
 {
  var row=await executor.QuerySingleAsync("dbo.usp_ScheduleOverride_Cancel",p=>{
   Int(p,"@EmployeeId",employeeId);Big(p,"@OverrideId",overrideId);
   p.Add("@ExpectedRowVersion",SqlDbType.Binary,8).Value=ParseVersion(dto.RowVersion);},Map,ct);
  return row??throw new InvalidOperationException("Cancel procedure returned no row.");
 }
 public async Task<ResolvedEmployeeScheduleDto> ResolveAsync(int employeeId,DateOnly workDate,CancellationToken ct)=>
  await executor.QuerySingleAsync("dbo.usp_EmployeeSchedule_Resolve",p=>{Int(p,"@EmployeeId",employeeId);Date(p,"@WorkDate",workDate);},MapResolved,ct)
  ??throw new InvalidOperationException("Schedule resolution returned no row.");
}
