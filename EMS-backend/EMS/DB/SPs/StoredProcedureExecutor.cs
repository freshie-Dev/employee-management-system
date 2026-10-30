
using System.Data;
using Microsoft.Data.SqlClient;

namespace EMS.DB.SPs;

public sealed class StoredProcedureExecutor(
    IConfiguration configuration
) : IStoredProcedureExecutor
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DbConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DbConnection' is missing.");

    private async Task<SqlConnection> OpenConnectionAsync(
        CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        string procedure,
        Action<SqlParameterCollection>? parameters)
    {
        var command = new SqlCommand(procedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        parameters?.Invoke(command.Parameters);

        return command;
    }

    public async Task<List<T>> QueryAsync<T>(
        string procedure,
        Action<SqlParameterCollection>? parameters,
        Func<SqlDataReader, T> map,
        CancellationToken ct = default)
    {
        await using var connection =
            await OpenConnectionAsync(ct);

        await using var command = CreateCommand(
            connection, procedure, parameters);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        var results = new List<T>();

        while (await reader.ReadAsync(ct))
        {
            results.Add(map(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleAsync<T>(
        string procedure,
        Action<SqlParameterCollection>? parameters,
        Func<SqlDataReader, T> map,
        CancellationToken ct = default)
    {
        var results = await QueryAsync(
            procedure,
            parameters,
            map,
            ct);

        return results.Count > 0
            ? results[0]
            : default;
    }

    public async Task<int> ExecuteAsync(
        string procedure,
        Action<SqlParameterCollection>? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection =
            await OpenConnectionAsync(ct);

        await using var command = CreateCommand(
            connection, procedure, parameters);

        return await command.ExecuteNonQueryAsync(ct);
    }


    public async Task<TResult> ExecuteWithOutputAsync<TResult>(
        string procedure,
        Action<SqlParameterCollection> parameters,
        Func<SqlDataReader, TResult> map,
        Func<TResult?, SqlParameterCollection, TResult> resolve,
        CancellationToken ct = default)
        where TResult : class
    {
        await using var connection =
            await OpenConnectionAsync(ct);

        await using var command = CreateCommand(
            connection, procedure, parameters);

        TResult? result = null;

        await using (var reader =
            await command.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                result = map(reader);
            }
        }

        // Output parameters are now populated.
        return resolve(result, command.Parameters);
    }

}
