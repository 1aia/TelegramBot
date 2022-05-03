using TelegramBot.Models;

namespace TelegramBot.Services;

public interface IMenuService
{
    string Name { get; }

    string Command { get; }

    Task<MenuServiceResponse> ProcessCommandAsync(string[] commandParts, bool isAdmin);

    Task<MenuServiceResponse> InitResponseAsync();
}
