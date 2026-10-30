
using Microsoft.Data.SqlClient;

namespace EMS.DB.SPs;

public interface IStoredProcedureExecutor
{
    Task<List<T>> QueryAsync<T>(
        string procedure,
        Action<SqlParameterCollection>? parameters,
        Func<SqlDataReader, T> map,
        CancellationToken ct = default);

    Task<T?> QuerySingleAsync<T>(
        string procedure,
        Action<SqlParameterCollection>? parameters,
        Func<SqlDataReader, T> map,
        CancellationToken ct = default);

    Task<int> ExecuteAsync(
        string procedure,
        Action<SqlParameterCollection>? parameters = null,
        CancellationToken ct = default);


    Task<TResult> ExecuteWithOutputAsync<TResult>(
        string procedure,
        Action<SqlParameterCollection> parameters,
        Func<SqlDataReader, TResult> map,
        Func<TResult?, SqlParameterCollection, TResult> resolve,
        CancellationToken ct = default)
        where TResult : class;

}
