using DMsgBot.Enums;
using DMsgBot.Interfaces;
using TL;

namespace DMsgBot
{
    public class TelegramMessage : ITelegramMessage
    {
        public Message Message { get; set; }
        public ETelegramMessageFrom MessageFrom { get; set; }
        public DateTime MessageDate { get; set; }

        public TelegramMessage(Message message, ETelegramMessageFrom messageFrom)
        {
            Message = message;
            MessageFrom = messageFrom;
            MessageDate = DateTime.Now;
        }
    }
}
