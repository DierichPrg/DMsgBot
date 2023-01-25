using DMsgBot.Attributes;
using DMsgBot.Interfaces;
using TL;

namespace DMsgBot.Commands
{
    [CommandName("/gatinha", "Envia pro guaxinim 'te amo muito <3' 10 vezes")]
    public class TelegramGatinhaCommand : TelegramCommandBase
    {
        public TelegramGatinhaCommand(TelegramBot telegramBot)
        : base(telegramBot)
        {
        }

        public override async void TelegramBotOnOnCommandMessage(object? sender, ITelegramMessage e)
        {
            long idGatinha = 1380874539;
            for (int i = 0; i < 10; i++)
                await this.TelegramClient.SendMessageAsync(new InputPeerUser(idGatinha, 0), "te amo muito <3");
        }

        public User? Receiver { get; set; }
        public override Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
