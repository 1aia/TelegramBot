namespace TelegramBot.Services.CoinMarketCap.ApiClient.Dto;

public class CryptoCurrency
{
    public string Name { get; set; }
    public int Rank { get; set; }
    public decimal Price { get; set; }
    public decimal DailyPercentageChange { get; set; }

}
