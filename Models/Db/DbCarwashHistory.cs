using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBot.Models.Db
{
    public class DbCarwashHistory
    {
        public DateTime CreatedAt { get; set; }

        public int Change { get; set; }

        public int Balance { get; set; }
    }
}
