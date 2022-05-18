namespace TelegramBot.Services.CoinMarketCap.ApiClient.ApiDto;

public class KeyInfoDto
{
    public KeyInfoUsageDto Usage { get; set; }
}

public class KeyInfoUsageDto
{   
    public KeyInfoCreditUsageDto current_day { get; set; }
    public KeyInfoCreditUsageDto current_month { get; set; }
}

public class KeyInfoCreditUsageDto
{
    public int credits_used { get; set; }
    public int credits_left { get; set; }
}

