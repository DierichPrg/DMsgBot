using System.Reflection;
using DMsgBot.Attributes;
using DMsgBot.Interfaces;
using Newtonsoft.Json.Linq;
using TL;
using WTelegram;

namespace DMsgBot.Commands
{
    public abstract class TelegramCommandBase : IDisposable
    {
        private TelegramBot TelegramBot { get; set; }

        public Chat ChatBotCenter => this.TelegramBot.BotCenterChat;
        
        public Client TelegramClient => this.TelegramBot.Client;

        public IEnumerable<User> Users => this.TelegramBot._users;
        
        public TelegramCommandBase(TelegramBot telegramBot, bool signEventHandler = true)
        {
            this.ValidClassAttribute();
            
            this.TelegramBot = telegramBot;

            if (signEventHandler)
                this.SignEventHandler();
        }

        public void SignEventHandler()
        {
            this.TelegramBot.OnCommandMessage += TelegramBotOnOnCommandMessage;
        }

        protected async Task SendToCenterChat(string message, int replyMsgId = 0)
        {
            await this.TelegramClient.SendMessageAsync(this.ChatBotCenter.ToInputPeer(), $"# {message}", reply_to_msg_id: replyMsgId);
        }

        protected void ValidClassAttribute()
        {
            if (this.GetType().GetCustomAttribute<CommandName>() is null)
                throw new Exception("Command class must have CommandName attribute");
        }

        public abstract void TelegramBotOnOnCommandMessage(object? sender, ITelegramMessage e);


        public virtual void Dispose()
        {
            this.TelegramBot.OnCommandMessage -= TelegramBotOnOnCommandMessage;
        }

        public abstract Task ExecuteAsync();
    }
}
