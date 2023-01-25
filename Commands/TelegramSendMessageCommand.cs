using System.Text;
using DMsgBot.Attributes;
using DMsgBot.Interfaces;
using static DMsgBot.Extensions.StringExtension;
using TL;

namespace DMsgBot.Commands
{
    [CommandName("/text", "Send a text message to a chat")]
    public class TelegramSendMessageCommand : TelegramCommandBase
    {
        public ESendMessageStep Step { get; private set; }

        public User? Receiver { get; set; }

        public IList<User>? PossibleReceivers { get; set; } = null;

        public string Message { get; set; }

        public TelegramSendMessageCommand(TelegramBot telegramBot)
        : base(telegramBot)
        {
            Step = ESendMessageStep.Start;
        }

        public override async void TelegramBotOnOnCommandMessage(object? sender, ITelegramMessage e)
        {
            byte? number = null;
            if (byte.TryParse(e.Message.message.Trim(), out byte n))
            {
                if (n == 0)
                {
                    if (this.Step == ESendMessageStep.DefineReceiver || this.Step == ESendMessageStep.Start)
                    {
                        this.Dispose();

                        await this.SendToCenterChat("Exited /text command.", e.Message.ID);

                        return;
                    }

                    this.Step = (ESendMessageStep)(byte)this.Step - 1;
                }
                else
                {
                    number = n;
                }
            }

            switch (this.Step)
            {
                case ESendMessageStep.Start:
                    {
                        var text = StringsToLines(
                            "To who do you wanna to send message?",
                            "You aways can go back sending 0 (zero)");

                        await this.SendToCenterChat(text, e.Message.ID);
                        this.Step = ESendMessageStep.DefineReceiver;
                    }
                    break;
                case ESendMessageStep.DefineReceiver:
                    {
                        if ((number ?? 0) > 0)
                        {
                            this.Receiver = this.PossibleReceivers?[(int)number! - 1];
                            this.Step = ESendMessageStep.DefineMessage;

                            var text = StringsToLines(
                                $"Ok, I will send message to {this.Receiver?.first_name} {this.Receiver?.last_name}.",
                                "What message do you wanna to send?");

                            await this.SendToCenterChat(text, e.Message.ID);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"We found those contacts, please send the number that you want.");
                            sb.AppendLine();
                            
                            this.PossibleReceivers = this.Users.Where(x => (x.first_name ?? "").Contains(e.Message.message.Trim()) || (x.last_name ?? "").Contains(e.Message.message.Trim())).ToList();

                            for (int i = 0; i < this.PossibleReceivers?.Count; i++)
                            {
                                sb.AppendLine($"{i + 1} - {this.PossibleReceivers?[i]?.first_name} {this.PossibleReceivers?[i]?.last_name}");
                                sb.AppendLine();
                            }

                            await this.SendToCenterChat(sb.ToString(), e.Message.ID);
                        }
                    }
                    break;
                case ESendMessageStep.DefineMessage:
                    {
                        this.Message = e.Message.message;
                        this.Step = ESendMessageStep.DefineQuantity;

                        await this.SendToCenterChat($"How much time do you want to send this message ( 1 - 255 )? send only number. Or 0 to cancel sending message.", e.Message.ID);
                    }
                    break;
                case ESendMessageStep.DefineQuantity:
                    {
                        if ((number ?? 0) > 0 && (number ?? 0) <= 255)
                        {
                            for (int i = 0; i < number; i++)
                                await this.TelegramClient.SendMessageAsync(this.Receiver!.ToInputPeer(), this.Message);

                            await this.SendToCenterChat($"Message sent {number} times.", e.Message.ID);
                            this.Dispose();
                        }
                        else
                        {
                            await this.SendToCenterChat($"Please send a number between 1 and 255.", e.Message.ID);
                        }
                    }
                    break;
            }
        }


        public override Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }

    public enum ESendMessageStep : byte
    {
        Start = 0,
        DefineReceiver,
        DefineMessage,
        DefineQuantity,
    }
}
