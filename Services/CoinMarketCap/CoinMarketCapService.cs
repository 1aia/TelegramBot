using System.Globalization;
using TelegramBot.Models;
using TelegramBot.Services.CoinMarketCap;
using TelegramBot.Services.CoinMarketCap.ApiClient.Dto;

namespace TelegramBot.Services;

public class CoinMarketCapService : IMenuService
{
    private readonly CoinMarketCapApiClient _apiClient;

    public CoinMarketCapService(CoinMarketCapApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public string Name => "CoinMarketCap";
    public string Command => "coinmarketcap";
     
    public async Task<MenuServiceResponse> InitResponseAsync()
    {
        var currenciesTask =  _apiClient.GetCurrenciesAsync();
        var marketTask = _apiClient.GetMarketAsync();
        var keyInfoTask = _apiClient.GetKeyInfoAsync();

        Task.WaitAll(currenciesTask, marketTask, keyInfoTask);

        var messageLines = new List<MessageLine>();

        messageLines.Add(GetMarketMessageLine(marketTask.Result));
        messageLines.AddRange(currenciesTask.Result.Select(GetCurrencyMessageLine));

        var text = string.Join("\n", GetStrings(messageLines.Where(x => x != null)).Append(GetKeyInfoString(keyInfoTask.Result)));

        return new MenuServiceResponse
        {
            NewMessage = new TextMessage
            {
                Text = $"<pre>{text}</pre>",
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            }
        };
    }

    private MessageLine GetMarketMessageLine(Market market)
    {
        if (market == null)
        {
            return null;
        }

        return GetMessageLine("Cap", market.Total / 1000000000, market.DailyPercentageChange);
    }

    private MessageLine GetCurrencyMessageLine(CryptoCurrency currency)
    {
        return GetMessageLine($"{currency.Name} [{currency.Rank}]", currency.Price, currency.DailyPercentageChange);
    }

    private MessageLine GetMessageLine(string title, decimal price, decimal change)
    {
        return new MessageLine
        {
            Title = title,
            Price = DecimalToString(price),
            Change = $"({DecimalToString(change, true)}%)"
        };
    }

    private IEnumerable<string> GetStrings(IEnumerable<MessageLine> messageLines)
    {
        var maxTitlePriceLength = messageLines.Max(x => x.Title.Length) + messageLines.Max(x => x.Price.Length);

        var res = messageLines.Select(messageLine =>
        {
            var places = maxTitlePriceLength - messageLine.Title.Length - messageLine.Price.Length;

            var titleToPricePlaceholder = string.Join("", Enumerable.Range(1, places).Select(x => " "));            

            return $"{messageLine.Title} {titleToPricePlaceholder}{messageLine.Price} {messageLine.Change}";
        });

        return res;
    }

    private string GetKeyInfoString(KeyInfo keyInfo)
    {
        if (keyInfo == null)
        {
            return string.Empty;
        }

        return $"Quota {keyInfo.DailyUsed}/{keyInfo.DailyUsed + keyInfo.DailyLeft} {keyInfo.MonthlyUsed}/{keyInfo.MonthlyUsed + keyInfo.MonthlyLeft}";
    }

    private string DecimalToString(decimal value, bool addPlusSign = false)
    {
        var nfi = new NumberFormatInfo { NumberGroupSeparator = " " };

        var strValue = value.ToString("#,##0.##", nfi);

        if (!addPlusSign)
        {
            return strValue;
        }

        return value > 0 ? $"+{strValue}": strValue;
    }

    public async Task<MenuServiceResponse> ProcessCommandAsync(string[] commandParts, bool isAdmin)
    {
        return new MenuServiceResponse();
    }

    class MessageLine
    {
        public string Title { get; set; }

        public string Price { get; set; }

        public string Change { get; set; }
    }
}
