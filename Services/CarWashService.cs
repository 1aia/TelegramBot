using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.DataAccess;
using TelegramBot.Models;
using TelegramBot.Models.Db;

namespace TelegramBot.Services;

public class CarWashService: IMenuService
{
    private readonly IDataRepository _repository;
    private readonly ILogger<HandleUpdateService> _logger;

    public CarWashService(ILogger<HandleUpdateService> logger, IDataRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public string Name => Selfwash.Name;
    public string Command => Selfwash.Command;

    private string BuildCommand(string cmd) => $"{Command} {cmd}";

    static class Selfwash
    {
        public const string Name = "CarWash";
        public const string Command = "selfwash";
        public const string Minus = "minus";
        public const string Plus = "plus";
        public const string History = "history";
        public const string ExportHistory = "export-history";
        public const string MainMenu = "main-menu";
        public const string Stats = "stats";
        public const string Close = "close";
        public const string MainMenuHeaderText = "Choose action";
    }

    private async Task<DbCarwashHistory?> GetLastHistoryItemAsync()
    {
        return (await _repository.GetCarwashHistoryAsync(1)).FirstOrDefault();
    }

    private async Task<InlineKeyboardMarkup> GetInlineBalanceKeyboardAsync()  
    {
        return GetInlineBalanceKeyboard(await GetLastHistoryItemAsync());
    }

    private InlineKeyboardMarkup GetInlineBalanceKeyboard(DbCarwashHistory? lastHistoryItem)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        var balance = lastHistoryItem?.Balance ?? 0;

        if (balance >= 25)
        {
            var minusActions = new List<InlineKeyboardButton>();
            buttons.Add(minusActions);

            minusActions.Add(InlineKeyboardButton.WithCallbackData("-25", BuildCommand($"{Selfwash.Minus} 25")));

            if (balance >= 50)
            {
                minusActions.Add(InlineKeyboardButton.WithCallbackData("-50", BuildCommand($"{Selfwash.Minus} 50")));
            }
            if (balance >= 100)
            {
                minusActions.Add(InlineKeyboardButton.WithCallbackData("-100", BuildCommand($"{Selfwash.Minus} 100")));
            }
        }

        var plusButtons = new List<InlineKeyboardButton>();
        buttons.Add(plusButtons);

        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+25", BuildCommand($"{Selfwash.Plus} 25")));
        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+100", BuildCommand($"{Selfwash.Plus} 100")));
        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+1000", BuildCommand($"{Selfwash.Plus} 1000")));

        var menuLastRow = new List<InlineKeyboardButton>();
        if (lastHistoryItem != null)
        {
            var historyButton = InlineKeyboardButton.WithCallbackData("History", BuildCommand(Selfwash.History));
            menuLastRow.Add(historyButton);

            var statsButton = InlineKeyboardButton.WithCallbackData("Stats", BuildCommand(Selfwash.Stats));
            menuLastRow.Add(statsButton);
        }

        var closeButton = InlineKeyboardButton.WithCallbackData("Close", BuildCommand(Selfwash.Close));
        menuLastRow.Add(closeButton);

        buttons.Add(menuLastRow);

        return new InlineKeyboardMarkup(buttons);
    }


    public async Task<MenuServiceResponse> InitResponseAsync()
    {
        return new MenuServiceResponse
        {
            NewMessage = await BuildMainMenuTextMessageAsync(),
        };
    }

    private async Task<TextMessage> BuildMainMenuTextMessageAsync()
    {
        var replyMarkup = await GetInlineBalanceKeyboardAsync();

        return new TextMessage
        {
            Text = Selfwash.MainMenuHeaderText,
            ReplyMarkup = replyMarkup
        };
    }

    public async Task<MenuServiceResponse> ProcessCommandAsync(string[] commandParts)
    {
        var response = new MenuServiceResponse();

        if (commandParts.Length < 2)
        {
            return response;
        }

        response = commandParts[1] switch
        {
            Selfwash.Minus => await ProcessBalanceChangeAsync(-1, commandParts),
            Selfwash.Plus => await ProcessBalanceChangeAsync(1, commandParts),
            Selfwash.History => await ProcessHistoryAsync(),
            Selfwash.Stats => await ProcessStatsAsync(),
            Selfwash.ExportHistory => await ProcessExportHistoryAsync(),
            Selfwash.MainMenu => new MenuServiceResponse { EditedMessage = await BuildMainMenuTextMessageAsync() },
            Selfwash.Close => new MenuServiceResponse { IsMessageDeleted = true },
            _ => response
        };

        return response;
    }

    private async Task<MenuServiceResponse> ProcessBalanceChangeAsync(int multiplier, string[] commandParts)
    {
        var response = new MenuServiceResponse();
        var change = commandParts.Length < 3 ? string.Empty : commandParts[2];

        if (int.TryParse(change, out var value))
        {
            var lastHistoryItem = await GetLastHistoryItemAsync();
            var initialBalance = lastHistoryItem?.Balance ?? 0;
            var initialButtons = GetInlineBalanceKeyboard(lastHistoryItem).InlineKeyboard.Sum(x => x.Count());
            
            var receivedValue = multiplier * value;
            var resultBalance = initialBalance + receivedValue;

            await _repository.CreateCarwashHistoryAsync(receivedValue);

            response.NewMessage = new TextMessage
            {
                Text = $"Received {receivedValue}. Balance {resultBalance}"
            };

            var resultButtons = (await GetInlineBalanceKeyboardAsync()).InlineKeyboard.Sum(x => x.Count());

            if (initialButtons != resultButtons)
            {
                response.EditedMessage = await BuildMainMenuTextMessageAsync();
            }
        }
        else
        {
            response.Answer = new CallbackQueryAnswer
            {
                Text = $"Received {string.Join(' ', commandParts)}. Unrecognized {Name} value"
            };
        }

        return response;
    }
    private async Task<InlineKeyboardMarkup> GetHistoryKeyboardAsync()
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        var pageSize = 5;
        var items = await _repository.GetCarwashHistoryAsync(pageSize);

        foreach (var historyItem in items)
        {
            var text = $"{historyItem.CreatedAt}: {historyItem.Change} => {historyItem.Balance}";

            buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text, BuildCommand(Selfwash.MainMenu)) });
        }

        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Export history", BuildCommand(Selfwash.ExportHistory)),
            InlineKeyboardButton.WithCallbackData("Back", BuildCommand(Selfwash.MainMenu))
        });

        return new InlineKeyboardMarkup(buttons);
    }

    private async Task<MenuServiceResponse> ProcessHistoryAsync()
    {
        var response = new MenuServiceResponse();

        response.EditedMessage = new TextMessage
        {
            Text = "History",
            ReplyMarkup = await GetHistoryKeyboardAsync()
        };

        return response;
    }

    private async Task<MenuServiceResponse> ProcessStatsAsync()
    {
        var response = new MenuServiceResponse();

        var dbItems = await _repository.GetCarwashHistoryAsync();
        var totalSpent = dbItems.Where(x => x.Change < 0).Sum(x => -x.Change);

        response.Answer = new CallbackQueryAnswer { Text = $"Total spent: {totalSpent}" };

        return response;
    }

    private async Task<MenuServiceResponse> ProcessExportHistoryAsync()
    {
        var response = new MenuServiceResponse();

        var dbItems = await _repository.GetCarwashHistoryAsync();
        var items = dbItems.Select(x => $"[{x.CreatedAt}] {x.Change} => {x.Balance}");
        var exportText = string.Join("\r\n", items);
        var today = DateTime.UtcNow;
        var fileName = $"history {today:dd.MM.yyyy HH-mm}.txt";

        response.Document = new Document
        {
            Data = Encoding.UTF8.GetBytes(exportText),
            FileName = fileName
        };

        return response;
    }
}
