using TelegramBot.Models.Db;

namespace TelegramBot.DataAccess;
public interface IDataRepository
{
    Task<int> GetCarwashLastBalanceAsync();

    Task<IEnumerable<DbCarwashHistory>> GetCarwashHistoryAsync(int? take = null, int skip = 0);

    Task<int> CreateCarwashHistoryAsync(int change);
}
