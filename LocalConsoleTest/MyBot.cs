using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Game = LocalConsoleTest.Data.Models.Game;

namespace LocalConsoleTest;

internal class MyBot : TelegramBotClient, IUpdateHandler, IUpdateReceiver {
    private readonly ILogger<MyBot> _logger;
    private readonly IServiceProvider _svcp;

    //public MyBot(TelegramBotClientOptions options, HttpClient? httpClient = null) : base(options, httpClient) { }

    //public MyBot(string token, HttpClient? httpClient = null) : base(token, httpClient) { }

    [UsedImplicitly]
    public MyBot(IOptions<TelegramOptions> opts, IServiceProvider svcp, ILogger<MyBot> logger) :
        base(opts.Value.ApiKey) {
        _svcp = svcp;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken) {
        using var scope = _svcp.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<Repository>();
        var gameManager = scope.ServiceProvider.GetRequiredService<GameManager>();
        var messageOrEditedMessage = update.Message ?? update.EditedMessage;

        if (messageOrEditedMessage?.From is null) {
            return;
        }

        var chatId = messageOrEditedMessage.Chat.Id;
        var threadId = messageOrEditedMessage.MessageThreadId ?? 0;
        _logger.LogInformation("chatid {chatId} {threadId}", chatId, messageOrEditedMessage.MessageThreadId);
        var game = await repository.GetRunningGame(chatId, threadId, cancellationToken);

        var fromName = new[] { messageOrEditedMessage.From.FirstName, messageOrEditedMessage.From.LastName }
            .Where(r => r is not null)
            .DefaultIfEmpty("John Doe").Aggregate((x, y) => $"{x} {y}")!;

        var context = new GameContext(
            Game: game,
            BotClient: botClient,
            CancellationToken: cancellationToken,
            TgMessage: messageOrEditedMessage,
            IsEdit: update.EditedMessage is not null,
            IsPrivateMessage: messageOrEditedMessage.Chat.Type == ChatType.Private,
            SenderId: messageOrEditedMessage.From.Id,
            SenderUsername: messageOrEditedMessage.From.Username,
            IsMemberAddedNotification: messageOrEditedMessage.Type == MessageType.ChatMembersAdded,
            NewChatMembers: messageOrEditedMessage.NewChatMembers,
            MessageText: messageOrEditedMessage.Text,
            SenderFullName: fromName,
            SendingPlayer: game.Players.SingleOrDefault(r => r.TelegramId == messageOrEditedMessage.From.Id),
            TheDm: game.Players.SingleOrDefault(r => r.IsDm)
        );

        gameManager.Context = context;

        await gameManager.HandleUpdate();

        var privateMessage = gameManager.PrivateOutput.ToString();

        if (!string.IsNullOrWhiteSpace(privateMessage)) {
            await Prv(botClient, messageOrEditedMessage.From.Id, privateMessage, cancellationToken);
        }

        var publicMessage = gameManager.PublicOutput.ToString();

        if (!string.IsNullOrWhiteSpace(publicMessage)) {
            await Snd(botClient, game, publicMessage, cancellationToken);
        }

        await repository.Save(cancellationToken);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task ReceiveAsync(IUpdateHandler updateHandler, CancellationToken cancellationToken = new()) {
        return Task.CompletedTask;
    }


    private static async Task Prv(ITelegramBotClient botClient, long senderId, string msg,
        CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(senderId, msg,
            cancellationToken: cancellationToken, parseMode: ParseMode.Html);

    private static async Task Snd(ITelegramBotClient botClient, Game game, string msg,
        CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(game.TelegramChannelId, msg,
            cancellationToken: cancellationToken, parseMode: ParseMode.Html,
            messageThreadId: game.TelegramThreadId == 0 ? null : game.TelegramThreadId);
}
