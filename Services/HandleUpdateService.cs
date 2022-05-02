using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    private static List<SelfwashHistory> _selfwashHistory = new List<SelfwashHistory>
    {
        new SelfwashHistory { Balance = 1000, CreatedAt = DateTime.UtcNow },
    };

    static class Selfwash
    {
        public const string Name = "selfwash";
        public const string Minus = "minus";
        public const string Plus = "plus";
        public const string History = "history";
        public const string ExportHistory = "export-history";
        public const string MainMenu = "main-menu";
        public const string Stats = "stats";
        public const string Close = "close";
        public const string MainMenuHeaderText = "Choose action";
    }

    class SelfwashHistory
    {
        public DateTime CreatedAt { get; set; }
        public int? Change { get; set; }
        public int Balance { get; set; }
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    static int Balance => _selfwashHistory.LastOrDefault()?.Balance ?? 0;

    static InlineKeyboardMarkup GetInlineBalanceKeyboard()  
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        if (Balance >= 25)
        {
            var minusActions = new List<InlineKeyboardButton>();
            buttons.Add(minusActions);

            minusActions.Add(InlineKeyboardButton.WithCallbackData("-25", $"{Selfwash.Name} {Selfwash.Minus} 25"));

            if (Balance >= 50)
            {
                minusActions.Add(InlineKeyboardButton.WithCallbackData("-50", $"{Selfwash.Name} {Selfwash.Minus} 50"));
            }
            if (Balance >= 100)
            {
                minusActions.Add(InlineKeyboardButton.WithCallbackData("-100", $"{Selfwash.Name} {Selfwash.Minus} 100"));
            }
        }

        var plusButtons = new List<InlineKeyboardButton>();
        buttons.Add(plusButtons);

        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+25", $"{Selfwash.Name} {Selfwash.Plus} 25"));
        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+100", $"{Selfwash.Name} {Selfwash.Plus} 100"));
        plusButtons.Add(InlineKeyboardButton.WithCallbackData("+1000", $"{Selfwash.Name} {Selfwash.Plus} 1000"));

        var menuLastRow = new List<InlineKeyboardButton>();
        if (_selfwashHistory.Count > 1)
        {
            var historyButton = InlineKeyboardButton.WithCallbackData("History", $"{Selfwash.Name} {Selfwash.History}");
            menuLastRow.Add(historyButton);

            var statsButton = InlineKeyboardButton.WithCallbackData("Stats", $"{Selfwash.Name} {Selfwash.Stats}");
            menuLastRow.Add(statsButton);
        }

        var closeButton = InlineKeyboardButton.WithCallbackData("Close", $"{Selfwash.Name} {Selfwash.Close}");
        menuLastRow.Add(closeButton);

        buttons.Add(menuLastRow);

        return new InlineKeyboardMarkup(buttons);
    }

    static InlineKeyboardMarkup GetHistoryKeyboard() 
    {        
        var buttons = new List<List<InlineKeyboardButton>>();

        var pageSize = 5;
        var skip = _selfwashHistory.Count > pageSize ? _selfwashHistory.Count - pageSize: 0;

        foreach(var historyItem in _selfwashHistory.Skip(skip).Take(pageSize).Reverse())
        {
            var text = $"{historyItem.CreatedAt}: {historyItem.Change ?? 0} => {historyItem.Balance}";

            buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text, $"{Selfwash.Name} {Selfwash.MainMenu}") });
        }

        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Export history", $"{Selfwash.Name} {Selfwash.ExportHistory}"),
            InlineKeyboardButton.WithCallbackData("Back", $"{Selfwash.Name} {Selfwash.MainMenu}")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);
        if (message.Type != MessageType.Text)
            return;

        var action = message.Text!.Split(' ')[0] switch
        {
            $"/{Selfwash.Name}"     => SendSelfwashKeyboard(_botClient, message),
            _           => Default(_botClient, message)
        };
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {sentMessageId}",sentMessage.MessageId);

        static async Task<Message> SendSelfwashKeyboard(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: Selfwash.MainMenuHeaderText,
                                                  replyMarkup: GetInlineBalanceKeyboard());
        }

        static async Task<Message> Default(ITelegramBotClient bot, Message message)
        {
            var text = $"Balance: {Balance}";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: text);
        }
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        var parts = callbackQuery.Data?.Split(' ');
        var text = $"Received {callbackQuery.Data}";

        if (parts?.Length > 0)
        {
            if (parts[0] == Selfwash.Name)
            {
                text = $"Received {callbackQuery.Data}. Unrecognized {Selfwash.Name} value";

                var type = parts[1];

                switch (type)
                {
                    case Selfwash.Minus:
                    case Selfwash.Plus:
                        {
                            var multiplier = type switch
                            {
                                Selfwash.Minus => -1,
                                _ => 1
                            };

                            var initialBalance = Balance;
                            var initialButtons = GetInlineBalanceKeyboard().InlineKeyboard.Sum(x => x.Count());

                            if (int.TryParse(parts[2], out var value))
                            {
                                var receivedValue = multiplier * value;
                                var resultBalance = initialBalance + receivedValue;

                                _selfwashHistory.Add(new SelfwashHistory
                                {
                                    Balance = resultBalance,
                                    Change = receivedValue,
                                    CreatedAt = DateTime.UtcNow
                                });

                                text = $"Received {receivedValue}. Balance {resultBalance}";

                                await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: text);

                                var resultButtons = GetInlineBalanceKeyboard().InlineKeyboard.Sum(x => x.Count());

                                if (initialButtons != resultButtons)
                                {
                                    await RenderSelfwashMenuAsync(callbackQuery);
                                }

                                return;
                            }                            

                            break;
                        }

                        case Selfwash.History:
                            await _botClient.EditMessageTextAsync(
                            chatId: callbackQuery.Message.Chat.Id,
                            messageId: callbackQuery.Message.MessageId,
                            text: "History",
                            replyMarkup: GetHistoryKeyboard());

                            return;

                        case Selfwash.Stats:
                            text = $"Total spent: {_selfwashHistory.Where(x => x.Change < 0).Sum(x => -x.Change) }";
                            break;

                        case Selfwash.MainMenu:
                            await RenderSelfwashMenuAsync(callbackQuery);
                            return;

                        case Selfwash.ExportHistory:
                        {
                            var items = _selfwashHistory.Select(x => $"[{x.CreatedAt}] {x.Change ?? 0} => {x.Balance}");
                            var exportText = string.Join("\r\n", items);
                            var today = DateTime.UtcNow;
                            var fileName = $"history{today:dd.MM.yyyy HH-mm}.txt";

                            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(exportText));
                            await _botClient.SendDocumentAsync(
                                chatId: callbackQuery.Message.Chat.Id,
                                document: new InputOnlineFile(stream, fileName));
                            return;
                        }                            

                        case Selfwash.Close:
                            await _botClient.DeleteMessageAsync(
                                chatId: callbackQuery.Message.Chat.Id,
                                messageId: callbackQuery.Message.MessageId);
                            return;
                }
            }
        }

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: text);
    }

    private async Task RenderSelfwashMenuAsync(CallbackQuery callbackQuery)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: Selfwash.MainMenuHeaderText,
            replyMarkup: GetInlineBalanceKeyboard());
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {updateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
}
