using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

/// <summary>
/// Discovers attendance work in pages and delegates actual evaluation to the
/// EXISTING evaluator. No attendance-calculation rules are duplicated here.
/// </summary>
public sealed class AttendanceBatchService(
    IStoredProcedureExecutor executor,
    IAttendanceEvaluationService evaluator,
    ILogger<AttendanceBatchService> logger) : IAttendanceBatchService
{
    private static AttendanceBatchCandidateDto MapCandidate(SqlDataReader r)
    {
        var endIndex = r.GetOrdinal("ScheduledEndAt");
        var recordIndex = r.GetOrdinal("ExistingAttendanceId");

        return new AttendanceBatchCandidateDto(
            r.GetInt32(r.GetOrdinal("EmployeeId")),
            DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("WorkDate"))),
            r.GetString(r.GetOrdinal("Decision")),
            r.GetString(r.GetOrdinal("ReasonCode")),
            r.IsDBNull(endIndex) ? null : r.GetDateTimeOffset(endIndex),
            r.IsDBNull(recordIndex) ? null : r.GetInt32(recordIndex),
            r.GetInt32(r.GetOrdinal("PunchCount"))
        );
    }

    public async Task<AttendanceBatchResultDto> RunAsync(
        DateOnly workDate,
        bool dryRun,
        int actorUserId,
        int maxEmployees,
        CancellationToken cancellationToken)
    {
        if (workDate == default)
            throw new ArgumentException("WorkDate is required.", nameof(workDate));
        if (actorUserId <= 0)
            throw new ArgumentException("A valid actor user ID is required.", nameof(actorUserId));
        if (maxEmployees is < 1 or > 5000)
            throw new ArgumentOutOfRangeException(nameof(maxEmployees));

        var results = new List<AttendanceBatchEmployeeResultDto>();
        var scanned = 0;
        var eligible = 0;
        var processed = 0;
        var skipped = 0;
        var failed = 0;
        var afterEmployeeId = 0;
        var limitReached = false;

        while (scanned < maxEmployees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var take = Math.Min(25, maxEmployees - scanned);

            var candidates = await executor.QueryAsync(
                "dbo.usp_AttendanceBatch_Candidates",
                p =>
                {
                    p.Add("@WorkDate", SqlDbType.Date).Value =
                        workDate.ToDateTime(TimeOnly.MinValue);
                    p.Add("@AfterEmployeeId", SqlDbType.Int).Value = afterEmployeeId;
                    p.Add("@Take", SqlDbType.Int).Value = take;
                },
                MapCandidate,
                cancellationToken);

            if (candidates.Count == 0)
                break;

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanned++;
                afterEmployeeId = candidate.EmployeeId;

                if (candidate.Decision == "ERROR")
                {
                    failed++;
                    results.Add(new(candidate.EmployeeId, "FAILED", candidate.ReasonCode));
                    continue;
                }

                if (candidate.Decision != "PROCESS")
                {
                    skipped++;
                    results.Add(new(candidate.EmployeeId, "SKIPPED", candidate.ReasonCode));
                    continue;
                }

                eligible++;
                if (dryRun)
                {
                    results.Add(new(candidate.EmployeeId, "WOULD_PROCESS", candidate.ReasonCode));
                    continue;
                }

                try
                {
                    var evaluated = await evaluator.EvaluateAsync(
                        candidate.EmployeeId,
                        workDate,
                        true,
                        actorUserId,
                        cancellationToken);

                    processed++;
                    results.Add(new(
                        candidate.EmployeeId,
                        "PROCESSED",
                        candidate.ReasonCode,
                        evaluated.AttendanceStatus));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (SqlException ex) when (ex.Number is 51406 or 51408)
                {
                    // Another worker may have finalized the record after discovery.
                    skipped++;
                    results.Add(new(candidate.EmployeeId, "SKIPPED_CONCURRENTLY", "ALREADY_PROCESSED"));
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogError(ex,
                        "Attendance batch failed for EmployeeId {EmployeeId}, WorkDate {WorkDate}",
                        candidate.EmployeeId, workDate);
                    results.Add(new(candidate.EmployeeId, "FAILED", "EVALUATION_ERROR"));
                }
            }

            if (candidates.Count < take)
                break;
        }

        // No full employee count query; true means the cap may have cut off rows.
        limitReached = scanned >= maxEmployees;

        return new AttendanceBatchResultDto(
            workDate, dryRun, scanned, eligible,
            processed, skipped, failed, limitReached, results);
    }
}
