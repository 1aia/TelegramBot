using TelegramBot.Models;

namespace TelegramBot.Services;

public class EmptyService : IMenuService
{
    public string Name => "EmptyService";
    public string Command => "empty";
    public async Task<MenuServiceResponse> InitResponseAsync()
    {
        return new MenuServiceResponse
        {
            NewMessage = new TextMessage
            {
                Text = "This is empty service"
            }
        };
    }

    public async Task<MenuServiceResponse> ProcessCommandAsync(string[] commandParts, bool isAdmin)
    {
        return new MenuServiceResponse();
    }
}
