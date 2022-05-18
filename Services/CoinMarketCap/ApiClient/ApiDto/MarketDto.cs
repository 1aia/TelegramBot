namespace TelegramBot.Services.CoinMarketCap.ApiClient.ApiDto;

public class MarketDto
{
    public Dictionary<string, MarketQuoteDto> Quote { get; set; }
}

public class MarketQuoteDto
{   
    public decimal total_market_cap { get; set; }
    public decimal total_market_cap_yesterday_percentage_change { get; set; }
}

