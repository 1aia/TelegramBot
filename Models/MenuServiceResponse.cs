using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Models;

public class MenuServiceResponse
{
    public TextMessage NewMessage { get; set; }

    public TextMessage EditedMessage { get; set; }

    public Document Document { get; set; }

    public CallbackQueryAnswer Answer { get; set; }

    public bool IsMessageDeleted { get; set; }
}

public class TextMessage
{
    public string Text { get; set; }

    public InlineKeyboardMarkup ReplyMarkup { get; set; }

    public ParseMode? ParseMode { get; set; }
}

public class CallbackQueryAnswer
{
    public string Text { get; set; }
}

public class Document
{
    public string FileName { get; set; }

    public byte[] Data { get; set; }
}