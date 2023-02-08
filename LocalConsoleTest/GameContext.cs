using LocalConsoleTest.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Game = LocalConsoleTest.Data.Models.Game;
using Message = Telegram.Bot.Types.Message;

namespace LocalConsoleTest;

internal record GameContext(
    Game Game,
    ITelegramBotClient BotClient,
    CancellationToken CancellationToken,
    Message TgMessage,
    bool IsEdit,
    bool IsPrivateMessage,
    long SenderId,
    string? SenderUsername,
    bool IsMemberAddedNotification,
    User[]? NewChatMembers,
    string? MessageText,
    string SenderFullName,
    Player? SendingPlayer,
    Player? TheDm);
