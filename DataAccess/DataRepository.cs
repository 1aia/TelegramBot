using TelegramBot.Models.Db;

namespace TelegramBot.DataAccess
{
    public class DataRepository : IDataRepository
    {
        private readonly IDataProvider _dataProvider;
        public DataRepository(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public async Task<int> GetCarwashLastBalanceAsync()
        {
            var sql = @$"
                SELECT h.balance
                FROM carwashhistory h 
                ORDER BY h.createdat desc";

            var res = await _dataProvider.QueryFirstOrDefaultAsync<int?>(sql);

            return res ?? 0;
        }

        public async Task<IEnumerable<DbCarwashHistory>> GetCarwashHistoryAsync(int? take = null, int skip = 0)
        {
            var sql = @$"
                SELECT h.createdat, h.change, h.balance
                FROM carwashhistory h 
                ORDER BY h.createdat desc
                LIMIT @limit
                OFFSET @offset";

            var param = new
            {
                offset = skip,
                limit = take == null ? int.MaxValue: take,
            };

            return await _dataProvider.QueryAsync<DbCarwashHistory>(sql, param);
        }

        public async Task<int> CreateCarwashHistoryAsync(int change)
        {
            var sql = @$"INSERT INTO public.carwashhistory
                (createdat, change, balance)
                VALUES(current_timestamp, @change, (coalesce((select balance from carwashhistory order by createdat desc limit 1), 0) + @change))";

            var param = new
            {                
                change = change
            };

            return await _dataProvider.ExecuteAsync(sql, param);
        }
    }
}