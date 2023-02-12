using LocalConsoleTest.Data;
using LocalConsoleTest.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalConsoleTest;

internal class Repository {
    private readonly MyDbContext _dc;

    public Repository(MyDbContext dc) {
        _dc = dc;
    }

    public async Task AddMessage(Game game, Player player, string text, int telegramMessageId,
        CancellationToken cancellationToken = default) {
        var msg = new Message {
            GameId = game.Id,
            PlayerId = player.Id,
            Text = text,
            TelegramMessageId = telegramMessageId,
            Turn = game.TurnNumber,
        };

        await _dc.Messages.AddAsync(msg, cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetMessages(Game game, int turn,
        CancellationToken cancellationToken = default) {
        var messages = await _dc.Messages
            .Include(r => r.Player)
            .Where(r => r.GameId == game.Id && r.Turn == turn)
            .OrderBy(r => r.TelegramMessageId)
            .ToListAsync(cancellationToken: cancellationToken);
        return messages;
    }

    public async Task<Message?> GetOriginalMessage(Game game, int telegramMessageId,
        CancellationToken cancellationToken = default) {
        var originalMessage =
            await _dc.Messages.SingleOrDefaultAsync(r =>
                r.GameId == game.Id && r.TelegramMessageId == telegramMessageId, cancellationToken: cancellationToken);

        return originalMessage;
    }

    public async Task<Person> GetPerson(long userId, CancellationToken cancellationToken = default) {
        var person = await _dc.Persons
            .Include(r => r.Players)
            .ThenInclude(r => r.Game)
            .SingleOrDefaultAsync(r => r.TelegramId == userId, cancellationToken: cancellationToken);

        if (person is not null) {
            return person;
        }

        var newPerson = new Person {
            InitiatedPrivateChat = false,
            TelegramId = userId,
        };

        await _dc.Persons.AddAsync(newPerson, cancellationToken);
        await _dc.SaveChangesAsync(cancellationToken);

        return newPerson;
    }

    public async Task<Game> GetRunningGame(long channelId, int threadId,
        CancellationToken cancellationToken = default) {
        var game = await _dc.Games
            .Include(r => r.Players)
            .FirstOrDefaultAsync(
                r => r.TelegramChannelId == channelId && r.TelegramThreadId == threadId && !r.IsArchived,
                cancellationToken: cancellationToken);

        if (game is not null) {
            return game;
        }

        var newGame = new Game {
            Name = "The Game",
            TurnNumber = 1,
            TelegramChannelId = channelId,
            TelegramThreadId = threadId,
        };
        await _dc.Games.AddAsync(newGame, cancellationToken);
        await _dc.SaveChangesAsync(cancellationToken);
        return newGame;
    }

    public async Task Save(CancellationToken cancellationToken = default) {
        await _dc.SaveChangesAsync(cancellationToken);
    }
}
