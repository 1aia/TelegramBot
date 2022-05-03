using Dapper;
using Npgsql;
using System.Data;
using TelegramBot.Configuration;

namespace TelegramBot.DataAccess;

public class DataProvider : IDataProvider
{
    private readonly string _connectionString;
    private readonly ILogger<DataProvider> _logger;
    public DataProvider(IConfiguration configuration, ILogger<DataProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connUrl = configuration[Literals.DbConnectionStringConfigKey];

        // Parse connection URL to connection string for Npgsql
        connUrl = connUrl.Replace("postgres://", string.Empty);

        var pgUserPass = connUrl.Split("@")[0];
        var pgHostPortDb = connUrl.Split("@")[1];
        var pgHostPort = pgHostPortDb.Split("/")[0];

        var pgDb = pgHostPortDb.Split("/")[1];
        var pgUser = pgUserPass.Split(":")[0];
        var pgPass = pgUserPass.Split(":")[1];
        var pgHost = pgHostPort.Split(":")[0];
        var pgPort = pgHostPort.Split(":")[1];

        _connectionString = $"Server={pgHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb}";
    }

    protected async Task<T> WithConnection<T>(Func<IDbConnection, Task<T>> func)
    {
        try
        {       
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                var res = await func(connection);

                return res;
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, exc.Message);
            throw;
        }
    }
    public async Task<int> ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action)
    {
        return await WithConnection(async connection =>
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    await action(connection, transaction);

                    transaction.Commit();

                    return 1;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        });
    }

    public async Task<int> ExecuteAsync(string sql, object param)
    {
        var res = await WithConnection(connection => connection.ExecuteAsync(sql, param));

        return res;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
    {
        var res = await WithConnection(connection => connection.QueryAsync<T>(sql, param));

        return res;
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null)
    {
        var res = await WithConnection(connection => connection.QueryFirstOrDefaultAsync<T>(sql, param));

        return res;
    }
}