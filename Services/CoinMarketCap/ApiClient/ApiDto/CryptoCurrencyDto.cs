namespace TelegramBot.Services.CoinMarketCap.ApiClient.ApiDto;

public class CryptoCurrencyDto
{
    public int? Cmc_Rank { get; set; }
    public Dictionary<string, QuoteDto> Quote { get; set; }
}

public class QuoteDto
{   
    public decimal Price { get; set; }
    public decimal? Percent_Change_24h { get; set; }
}

