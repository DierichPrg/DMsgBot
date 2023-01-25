using System.Collections.Concurrent;
using System.Reflection;
using DMsgBot.Attributes;
using DMsgBot.Commands;
using DMsgBot.Config;
using DMsgBot.Enums;
using DMsgBot.Interfaces;
using TL;
using WTelegram;
using static DMsgBot.Extensions.StringExtension;

namespace DMsgBot
{
    public class TelegramBot
    {
        // should have a group called "dbot" on telegram to this bot work.
        private const string botCenterChatTitle = "dbot";

        public Client Client { get; }
        public User User { get; set; }
        public TelegramConfig Config { get; }
        public Chat BotCenterChat { get; set; }

        private EventHandler<ITelegramMessage> _onCommandMessage;

        public event EventHandler<ITelegramMessage> OnCommandMessage
        {
            add
            {
                if (_onCommandMessage == null || !_onCommandMessage.GetInvocationList().Contains(value))
                    _onCommandMessage += value;
            }

            remove => _onCommandMessage -= value;
        }
        public void RaiseOnCommandMessage(ITelegramMessage message) => this._onCommandMessage?.Invoke(this, message);

        private EventHandler<ITelegramMessage> _onChatMessage;

        public event EventHandler<ITelegramMessage> OnChatMessage
        {
            add
            {
                if (_onChatMessage == null || !_onChatMessage.GetInvocationList().Contains(value))
                    _onChatMessage += value;
            }

            remove => _onChatMessage -= value;
        }

        public void RaiseOnChatMessage(ITelegramMessage message) => this._onChatMessage?.Invoke(this, message);

        private EventHandler<ITelegramMessage> _onGroupMessage;

        public event EventHandler<ITelegramMessage> OnGroupMessage
        {
            add
            {
                if (_onGroupMessage == null || !_onGroupMessage.GetInvocationList().Contains(value))
                    _onGroupMessage += value;
            }

            remove => _onGroupMessage -= value;
        }

        public void RaiseOnGroupMessage(ITelegramMessage message) => this._onGroupMessage?.Invoke(this, message);


        public bool HasCommandMessageHandlerSigned => this._onCommandMessage?.GetInvocationList()?.Length > 0;

        public readonly ConcurrentBag<Chat> _groups;
        public readonly ConcurrentBag<Channel> _channels;
        public readonly ConcurrentBag<User> _users;


        public TelegramBot()
        {
            this._channels = new ConcurrentBag<Channel>();
            this._groups = new ConcurrentBag<Chat>();
            this._users = new ConcurrentBag<User>();

            this.Config = new TelegramConfig();

            this.Client = new Client(this.Config.Config);
            this.Client.OnUpdate += Client_OnUpdate;
        }

        public async Task LoginAsync()
        {
            try
            {
                this.User = await this.Client.LoginUserIfNeeded();

                this.Config.SetFirstName(this.User.first_name);
                this.Config.SetLastName(this.User.last_name);

                Console.Clear();
                Console.WriteLine($"Welcome {this.User.first_name}, now you are logged in on Dietrich Bot Manager!");
                Console.WriteLine($"Don't be afraid, I don't log any kind of data.");
                Console.WriteLine(LineSeparator);
            }
            catch (TL.RpcException rcp)
            {
                if (rcp.Code != 400)
                    throw;

                Console.Clear();

                Console.WriteLine("Input verification code that Telegram just sent to you.");
                string verificationCode = (Console.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(verificationCode))
                {
                    Console.WriteLine("Invalid verification code.");
                    throw;
                }

                this.Config.SetVerificationCode(verificationCode);

                await this.LoginAsync();
            }
        }

        public async Task Initialize()
        {
            await this.SendToCenterChat(StringsToLines(
                "Welcome to DMsgBot!",
                "To see all commands, type /bot",
                "To exit a command, type /exit",
                "Let's try?"));
        }

        public async Task SendToCenterChat(string message, int replyMsgId = 0)
        {
            await this.Client.SendMessageAsync(this.BotCenterChat.ToInputPeer(), $"# {message}", reply_to_msg_id: replyMsgId);
        }

        public IEnumerable<string> GetAllTextWithDescriptionCommands()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                .Where(t =>
                    t.Namespace == $"{assembly.FullName.Split(',').First()}.Commands"
                    && t.BaseType == typeof(TelegramCommandBase)
                    && t.GetType() != typeof(TelegramCommandBase))
                .Select(x => x.GetCustomAttribute<CommandName>().Name + " -> " + x.GetCustomAttribute<CommandName>().Description)
                .ToList();
        }

        public Type? GetCommand(string commandName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                .Where(t =>
                    t.Namespace == $"{assembly.FullName.Split(',').First()}.Commands"
                    && t.BaseType == typeof(TelegramCommandBase)
                    && t.GetType() != typeof(TelegramCommandBase))
                .FirstOrDefault(x => x.GetCustomAttribute<CommandName>()?.Name.ToLower().Trim() == commandName.ToLower().Trim());
        }

        public async Task ReadAllDialogsChannelsAndGroups()
        {
            var allMessages = await this.Client.Messages_GetAllDialogs();

            foreach (var i in allMessages.chats)
            {
                if (i.Value is Chat group)
                    this._groups.Add(group);

                if (i.Value is Channel channel)
                    this._channels.Add(channel);
            }

            var c = this._groups.FirstOrDefault(x => x.Title.Trim().Equals(botCenterChatTitle));

            if (c is null)
            {
                Console.Clear();

                Console.WriteLine($"Its necessary have a group named '{botCenterChatTitle}' to manipulate this bot.");
                Console.WriteLine($"Please, create a group named '{botCenterChatTitle}' and start this program again.");
                Environment.Exit(1);
            }

            this.BotCenterChat = c;

            var contactsGetContacts = await this.Client.Contacts_GetContacts();

            foreach (var i in contactsGetContacts.users)
                this._users.Add(i.Value);
        }

        private async Task Client_OnUpdate(IObject arg)
        {
            if (this.BotCenterChat == null)
                return;

            if (arg is UpdatesBase updates)
            {
                var invoiceMessages = updates.UpdateList.Where(x => x is UpdateNewMessage).Select(x => x as UpdateNewMessage);

                var findMessageOnCenter = invoiceMessages
                    .Where(x => x.message.Peer.ID == this.BotCenterChat.ID)
                    .Select(x => x.message as Message);

                foreach (var i in findMessageOnCenter.Where(x => !x.message.StartsWith("#")))
                    await this.BotChannelHandler(i!);

                var findMessagesChat = invoiceMessages
                    .Where(x => x.message.Peer is PeerUser && x.message.Peer.ID != this.BotCenterChat.ID && x.message.From.ID != this.User.ID)
                    .Select(x => x.message as Message);

                foreach (var i in findMessagesChat)
                    this.RaiseOnChatMessage(new TelegramMessage(i!, ETelegramMessageFrom.Chat));

                var findMessagesGroup = invoiceMessages
                    .Where(x => (x.message.Peer is PeerChannel || x.message.Peer is PeerChat) && x.message.Peer.ID != this.BotCenterChat.ID && x.message.From.ID != this.User.ID)
                    .Select(x => x.message as Message);

                foreach (var i in findMessagesGroup)
                    this.RaiseOnChatMessage(new TelegramMessage(i!, ETelegramMessageFrom.Group));
            }
        }

        private async Task BotChannelHandler(Message msg)
        {
            if (this.BotCenterChat == null)
            {
                Console.WriteLine($"'{botCenterChatTitle}' dont found");
                return;
            }

            // ignora as mensagens que o bot mandou, por padrão sempre começa com # na frente
            if (msg.message.StartsWith("#"))
                return;

            string command = msg.message.Trim();

            if (!this.HasCommandMessageHandlerSigned)
            {
                if (command.ToLower().Equals("/bot"))
                {
                    await this.SendToCenterChat(StringsToLines("Availables commands:", this.GetAllTextWithDescriptionCommands().StringsToLines()), msg.ID);
                    return;
                }

                if (command.ToLower().Equals("/exit"))
                {
                    await this.SendToCenterChat("You not are in any command scope.", msg.ID);
                    return;
                }

                var commandType = this.GetCommand(command);
                if (commandType is null)
                {
                    await this.SendToCenterChat($"Dont found the command {command}", msg.ID);
                    return;
                }

                Activator.CreateInstance(commandType, this);
            }
            else
            {
                if (command.ToLower().Equals("/exit"))
                {
                    var commandName = this._onCommandMessage.GetInvocationList().First().Target.GetType().GetCustomAttribute<CommandName>().Name;
                    await this.SendToCenterChat($"Exited of command '{commandName}'", msg.ID);

                    this._onCommandMessage = null;
                    return;
                }
            }

            this.RaiseOnCommandMessage(new TelegramMessage(msg, ETelegramMessageFrom.CenterBot));
        }
    }


    // parte de codigo salvo



    //else // ja entrou em 1 comando
    //{
    //    switch (this.BuildingCommand.Item2)
    //    {
    //        case ETelegramCommandType.SendMessage:
    //            {
    //                var tsmc = this.BuildingCommand.Item3 as TelegramSendMessageCommand;

    //                if (tsmc.Receiver is null)
    //                {
    //                    if (int.TryParse(command.Trim(), out int index) && tsmc.Step == ESendMessageStep.DefineReceiver)
    //                    {
    //                        tsmc.Receiver = tsmc.PotentialReceiverers[index - 1];
    //                        tsmc.Step = ESendMessageStep.DefineMessage;

    //                        await this.SendToCenterChat($"Now write the message that you want to send.");

    //                        return;
    //                    }
    //                    else if (tsmc.Step == ESendMessageStep.DefineReceiver)
    //                    {
    //                        await this.SendToCenterChat($"We found those contacts, please send the number that you want.", msg.ID);
    //                        tsmc.PotentialReceiverers = this._users.Where(x => (x.first_name ?? "").Contains(command) || (x.last_name ?? "").Contains(command)).ToList();

    //                        StringBuilder sb = new StringBuilder();
    //                        foreach (var user in tsmc.PotentialReceiverers)
    //                            sb.AppendLine($"{sb.Length + 1} - {user.first_name} {user.last_name} | {user.username}");

    //                        await this.SendToCenterChat(sb.ToString());

    //                        return;
    //                    }
    //                }

    //                if (tsmc.Receiver is not null && tsmc.Step == ESendMessageStep.DefineMessage && string.IsNullOrEmpty(tsmc.Message))
    //                {
    //                    tsmc.Message = command;

    //                    await this.SendToCenterChat($"How much time do you want to send this message ( 1 - 255 )? send only number. Or 0 to cancel sending message.");
    //                    tsmc.Step = ESendMessageStep.DefineQuantity;

    //                    return;
    //                }

    //                if (tsmc.Receiver is not null && !string.IsNullOrEmpty(tsmc.Message) && tsmc.Step == ESendMessageStep.DefineQuantity && byte.TryParse(command, out byte qt))
    //                {
    //                    tsmc.QuantityToSent = qt;

    //                    for (int i = 0; i < tsmc.QuantityToSent; i++)
    //                        await this.Client.SendMessageAsync(tsmc.Receiver!.ToInputPeer(), tsmc.Message);

    //                    await this.SendToCenterChat($"{tsmc.QuantityToSent} messages sent to {tsmc.Receiver.first_name} with success!");

    //                    this.BuildingCommand = (false, ETelegramCommandType.None, null);

    //                    return;
    //                }
    //            }
    //            break;
    //        default:
    //            return;
    //    }
    //}



    //public void OpenBrowser(string url)
    //{
    //    try
    //    {
    //        Process.Start(url);
    //    }
    //    catch
    //    {
    //        // hack because of this: https://github.com/dotnet/corefx/issues/10361
    //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //        {
    //            url = url.Replace("&", "^&");
    //            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
    //        }
    //        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //        {
    //            Process.Start("xdg-open", url);
    //        }
    //        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //        {
    //            Process.Start("open", url);
    //        }
    //        else
    //        {
    //            throw;
    //        }
    //    }
    //}

    //var authExportLoginToken = (await this.client.Auth_ExportLoginToken(int.Parse(this.config.api_id), this.config.api_hash)) as TL.Auth_LoginToken;

    //var encode = "tg://login?token=" + Base64UrlEncoder.Encode(authExportLoginToken.token);

    //var qr = QrCode.EncodeText(encode, QrCode.Ecc.Medium);
    //string svg = qr.ToSvgString(4);

    //var path = Path.Combine(Environment.CurrentDirectory, "qrLoginTelegram.svg");

    //File.WriteAllText(path, svg, Encoding.UTF8);

    //this.OpenBrowser(path);
}
