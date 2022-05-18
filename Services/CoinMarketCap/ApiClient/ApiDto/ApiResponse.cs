namespace TelegramBot.Services.CoinMarketCap.ApiClient.ApiDto;

public class ApiResponse<T>
{    
    public ApiResponseStatus Status { get; set; }
    public T Data { get; set; }
}

public class ApiResponseStatus
{
    public DateTime Timestamp { get; set; }

    public int CreditCount { get; set; }
}
