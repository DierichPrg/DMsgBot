using DMsgBot;


var telegramBot = new TelegramBot();
await telegramBot.LoginAsync();
await telegramBot.ReadAllDialogsChannelsAndGroups();
await telegramBot.Initialize();

telegramBot.OnChatMessage += (sender, msg) => Console.WriteLine($"Msg recebida de: {msg.MessageFrom.ToString()} | {msg.Message.message} | as {msg.MessageDate.ToString("HH:mm:ss")}");
telegramBot.OnGroupMessage += (sender, msg) => Console.WriteLine($"Msg recebida de: {msg.MessageFrom.ToString()} | {msg.Message.message} | as {msg.MessageDate.ToString("HH:mm:ss")}");

while ((Console.ReadLine() ?? string.Empty).Trim() != "exit");
