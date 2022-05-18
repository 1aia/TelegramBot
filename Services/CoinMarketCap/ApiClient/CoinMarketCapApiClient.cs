using Flurl.Http;
using TelegramBot.Configuration;
using TelegramBot.Services.CoinMarketCap.ApiClient.ApiDto;
using TelegramBot.Services.CoinMarketCap.ApiClient.Dto;

namespace TelegramBot.Services.CoinMarketCap;

public class CoinMarketCapApiClient
{
    private readonly CoinMarketCapConfiguration _config;
    private readonly ILogger<CoinMarketCapApiClient> _logger;

    public CoinMarketCapApiClient(IConfiguration configuration, ILogger<CoinMarketCapApiClient> logger)
    {
        _config = configuration.GetSection(Literals.CoinMarketCapConfigurationKey).Get<CoinMarketCapConfiguration>();
        _logger = logger;
    }

    private async Task<Response<ApiResponse<T>>> ExecuteAsync<T>(string restUri)
    {
        var url = $"{_config.BaseUri}/{restUri}";

        var response = new Response<ApiResponse<T>>
        {
            Success = true
        };

        try
        {
            _logger.LogInformation($"Requesting {url}");

            response.Data = await url.WithHeader("X-CMC_PRO_API_KEY", _config.ApiKey).GetJsonAsync<ApiResponse<T>>();
        }
        catch(Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, $"{url} failed");
            response.Success = false;
        }       

        return response;

    }

    public async Task<IEnumerable<CryptoCurrency>> GetCurrenciesAsync()
    {
        var apiResponse = await ExecuteAsync<Dictionary<string, IEnumerable<CryptoCurrencyDto>>>("v2/cryptocurrency/quotes/latest?symbol=BTC,ETH,LTC,XMR");

        if (!apiResponse.Success)
        {
            return Enumerable.Empty<CryptoCurrency>();
        }

        var response = apiResponse.Data.Data.Select(x =>
        {
            var currency = x.Value.First();
            var quote = currency.Quote.FirstOrDefault().Value;

            return new CryptoCurrency
            {
                Name = x.Key,
                Rank = currency.Cmc_Rank,
                Price = quote?.Price ?? 0,
                DailyPercentageChange = quote?.Percent_Change_24h ?? 0
            };
        });

        return response;
    }

    public async Task<Market> GetMarketAsync()
    {
        var apiResponse = await ExecuteAsync<MarketDto>("v1/global-metrics/quotes/latest");

        if (!apiResponse.Success)
        {
            return null;
        }

        var market = apiResponse.Data.Data.Quote.FirstOrDefault().Value;

        return new Market
        {
            Total = market?.total_market_cap ?? 0,
            DailyPercentageChange = market?.total_market_cap_yesterday_percentage_change ?? 0
        };
    }

    public async Task<KeyInfo> GetKeyInfoAsync()
    {
        var apiResponse = await ExecuteAsync<KeyInfoDto>("v1/key/info");

        if (!apiResponse.Success)
        {
            return null;
        }

        var usage = apiResponse.Data.Data.Usage;;

        return new KeyInfo
        {
            DailyUsed = usage.current_day.credits_used,
            DailyLeft = usage.current_day.credits_left,
            MonthlyUsed = usage.current_month.credits_used,
            MonthlyLeft = usage.current_month.credits_left,
        };
    }

    private class Response<T>
    {
        public bool Success { get; set; }

        public T Data { get; set; }
    }
}
