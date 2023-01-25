using DMsgBot.Enums;
using TL;

namespace DMsgBot.Interfaces
{
    public interface ITelegramMessage
    {
        Message Message { get; set; }
        ETelegramMessageFrom MessageFrom { get; set; }
        DateTime MessageDate { get; set; }
    }
}
