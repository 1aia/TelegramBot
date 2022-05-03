using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Configuration;
using TelegramBot.Models;

namespace TelegramBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly Dictionary<string, IMenuService> _menuServicesDict;
    private int _adminId;

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger, 
        IEnumerable<IMenuService> menuServices, IConfiguration configuration)
    {
        _botClient = botClient;
        _logger = logger;
        _menuServicesDict = menuServices.ToDictionary(x => x.Command);
        _adminId = configuration.GetSection(Literals.AdminIdConfigurationKey).Get<int>();
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
    
    private async Task<Message> SendInitMessage(Message message, IMenuService menuService)
    {
        await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

        var response = await menuService.InitResponseAsync();

        return await OnMessageProcessedAsync(message, response);
    }

    private InlineKeyboardMarkup GetServicesMenuInlineKeyboard()
    {
        if (!_menuServicesDict.Any())
        {
            return null;
        }

        var buttons = new List<InlineKeyboardButton>();

        foreach (var button in _menuServicesDict)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(button.Value.Name, button.Value.Command));
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private async Task<Message> SendDefaultMessage(Message message)
    {
        var replyMarkup = GetServicesMenuInlineKeyboard();

        var text = replyMarkup != null ? "Active services" : "No service available";

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            replyMarkup: replyMarkup);
    }

    private async Task<Message> OnMessageProcessedAsync(Message message, MenuServiceResponse menuServiceResponse)
    {
        if (menuServiceResponse.NewMessage != null)
        {
            var msg = menuServiceResponse.NewMessage;

            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: msg.Text,
                replyMarkup: msg.ReplyMarkup);
        }

        if (menuServiceResponse.EditedMessage != null)
        {
            var msg = menuServiceResponse.EditedMessage;

            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: msg.Text,
                replyMarkup: msg.ReplyMarkup);
        }

        if (menuServiceResponse.Document != null)
        {
            var doc = menuServiceResponse.Document;

            using var stream = new MemoryStream(doc.Data);

            return await _botClient.SendDocumentAsync(
                chatId: message.Chat.Id,
                document: new InputOnlineFile(stream, doc.FileName));
        }

        return null;
    }

    private async Task OnCallbackQueryReceivedAsync(CallbackQuery callbackQuery, MenuServiceResponse menuServiceResponse)
    {
        var message = callbackQuery.Message;

        await OnMessageProcessedAsync(message, menuServiceResponse);

        if (menuServiceResponse.IsMessageDeleted)
        {
            await _botClient.DeleteMessageAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId);
        }

        if (menuServiceResponse.Answer != null)
        {
            var answer = menuServiceResponse.Answer;

            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: answer.Text);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);

        if (message.Type != MessageType.Text)
        {
            return;
        }

        var command = message.Text!.Split(' ')[0].Replace("/", "");

        Task<Message> action;

        if (_menuServicesDict.ContainsKey(command))
        {
            var service = _menuServicesDict[command];

            action = SendInitMessage(message, service);
        }
        else
        {
            action = SendDefaultMessage(message);
        }
        
        var sentMessage = await action;

        _logger.LogInformation("The message was sent with id: {sentMessageId}",sentMessage.MessageId);
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        var parts = callbackQuery.Data?.Split(' ');
        var text = $"Received {callbackQuery.Data}";
        var isAdmin = callbackQuery.From.Id == _adminId;

        if (parts?.Length > 0)
        {
            var serviceName = parts[0];

            if (_menuServicesDict.ContainsKey(serviceName))
            {
                var service = _menuServicesDict[serviceName];

                var response = parts.Length == 1 
                    ? await service.InitResponseAsync()
                    : await service.ProcessCommandAsync(parts, isAdmin);

                await OnCallbackQueryReceivedAsync(callbackQuery, response);
            }
        }
        else
        {
            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: text);
        }        
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
