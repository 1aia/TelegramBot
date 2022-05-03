using System.Data;

namespace TelegramBot.DataAccess;

public interface IDataProvider
{
    Task<int> ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action);

    Task<int> ExecuteAsync(string sql, object param = null);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);

    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null);
}