using System.Text;
using LocalConsoleTest.Data.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace LocalConsoleTest;

internal class GameManager {
    private readonly ILogger<GameManager> _logger;
    private readonly Repository _repository;
    private bool _anyPrivate;
    private bool _anyPublic;

    public GameManager(Repository repository, ILogger<GameManager> logger) {
        _repository = repository;
        _logger = logger;
    }

    public StringBuilder PublicOutput { get; } = new();
    public StringBuilder PrivateOutput { get; } = new();
    public GameContext Context { get; set; } = null!;

    public async Task HandleUpdate() {
        if (Context is { IsEdit: true, IsPrivateMessage: false }) {
            await HandleEdit();
            return;
        }

        if (Context.IsMemberAddedNotification) {
            await HandleMemberAdded();
        }
        else {
            _logger.LogInformation("{user}: {msg}", Context.SenderUsername, Context.MessageText);

            if (Context.IsPrivateMessage) {
                await HandlePrivateMessage();
            }
            else if (Context.MessageText == "/kick") {
                CmdKickNoArgs();
            }
            else if (Context.MessageText?.StartsWith("/kick ") ?? false) {
                CmdKickWithArgs();
            }
            else if (Context.MessageText == "/endgame") {
                CmdEndGameNoArgs();
            }
            else if (Context.MessageText == $"/endgame {Context.Game.ResetPassword}") {
                await CmdEndGameConfirmed();
            }
            else if (Context.MessageText?.StartsWith("/endgame ") ?? false) {
                CmdEndGameUnconfirmed();
            }
            else if (Context.MessageText == "/start") {
                CmdStartNoArgs();
            }
            else if (Context.MessageText == $"/start {Context.Game.ResetPassword}") {
                await CmdStartConfirmed();
            }
            else if (Context.MessageText?.StartsWith("/start ") ?? false) {
                CmdStartUnconfirmed();
            }
            else if (Context.MessageText == "/pause") {
                CmdPause();
            }
            else if (Context.MessageText == "/resume") {
                CmdResume();
            }
            else if (Context.MessageText == "/play") {
                CmdPlay();
            }
            else if (Context.MessageText == "/help") {
                CmdHelp();
            }
            else if (Context.MessageText == "/status") {
                CmdStatus();
            }
            else if (Context.MessageText == "/nudge") { }
            else if (Context.MessageText?.StartsWith("/showturn ") ?? false) {
                await CmdShowTurn();
            }
            else {
                await HandleGameMessage();
            }
        }
    }

    private void CheckIfEveryoneTalkedAndAdvanceTurn() {
        if (Context.Game.Players.All(r => r.Played)) {
            foreach (var player in Context.Game.Players) {
                player.Played = false;
            }

            Context.Game.TurnNumber++;
            Snd("All players have spoken, a new turn has begun!");
            Snd($"This is turn #{Context.Game.TurnNumber}");
        }
    }

    private async Task CmdEndGameConfirmed() {
        var any = Context.Game.Players.Any();
        if (!Context.Game.IsRunning && !any) {
            Snd("No game has been started yet.");
            return;
        }

        if (Context.SendingPlayer != Context.TheDm) {
            Snd("Only the DM may end the game.");
            return;
        }

        if (any) {
            // Archive existing game
            Context.Game.IsArchived = true;
            await _repository.Save();
            Context = Context with {
                Game = await _repository.GetRunningGame(Context.Game.TelegramChannelId, Context.Game.TelegramThreadId),
            };
        }

        Context.Game.IsRunning = false;
        Context.Game.Players.Clear();

        Snd("Game archived.");
    }

    private void CmdEndGameNoArgs() {
        if (!Context.Game.IsRunning && !Context.Game.Players.Any()) {
            Snd("No game has been started yet.");
            return;
        }

        if (Context.SendingPlayer != Context.TheDm) {
            Snd("Only the DM may end the game.");
            return;
        }

        var password = new Random().Next(1000, 10000).ToString();
        Snd("This is a serious action, this will end the game permanently and archive all messages. " +
            $"To make sure you really wanted to do that type /endgame {password}");
        Context.Game.ResetPassword = password;
    }

    private void CmdEndGameUnconfirmed() {
        Snd($"Wrong confirmation code. If you're really sure, then type /endgame {Context.Game.ResetPassword}");
    }

    private void CmdHelp() {
        Snd("General commands: /help /status /play /showturn");
        Snd("DM commands: /kick /pause /resume /endgame");
        Snd("To start a fresh game type /start");
    }

    private void CmdKickNoArgs() {
        if (!Context.Game.Players.Any()) {
            Snd("No game exists yet");
            return;
        }

        if (Context.SendingPlayer != Context.TheDm) {
            Snd("Only the DM can kick players from the game.");
            return;
        }

        var plrs = Context.Game.Players.Select((r, i) => $"{i + 1}. {Mention(r)}").Aggregate((x, y) => $"{x}\n{y}");
        Snd($"{plrs}");
        Snd();
        Snd("Use /kick ### (where ### is the number of player from the list)");
    }

    private void CmdKickWithArgs() {
        if (!int.TryParse(Context.MessageText![6..], out var plrNumber) || plrNumber < 1 ||
            plrNumber > Context.Game.Players.Count) {
            var plrs = Context.Game.Players.Select((r, i) => $"{i + 1}. {Mention(r)}").Aggregate((x, y) => $"{x}\n{y}");
            Snd(plrs);
            Snd();
            Snd("Use /kick ### (where ### is the number of player from the list)");
            return;
        }

        var plrRemoved = Context.Game.Players.ElementAt(plrNumber - 1);
        Context.Game.Players.Remove(plrRemoved);
        Snd($"{Mention(plrRemoved)} has been removed from the game (but may still rejoin).");

        CheckIfEveryoneTalkedAndAdvanceTurn();
    }

    private void CmdPause() {
        if (!Context.Game.IsRunning) {
            Snd("Game is not running.");
            return;
        }

        if (!(Context.SendingPlayer?.IsDm ?? false)) {
            Snd("Only the DM can pause the game.");
            return;
        }

        Context.Game.IsRunning = false;

        Snd("Game paused, everyone may speak freely, messages won't be recorded.");
    }

    private void CmdPlay() {
        if (!Context.Game.IsRunning && !Context.Game.Players.Any()) {
            Snd("Game has not been started in this channel yet. " +
                "To start the game, the designated DM should type /start");
            return;
        }

        if (Context.SendingPlayer is not null) {
            Snd(Context.SendingPlayer.Played
                ? "You are already in the game, and you have already spoken this turn."
                : "You are already in the game, and you still may speak this turn.");
            return;
        }

        var canSpeak = Context.Game.Players.All(r => !r.Played);

        Context.Game.Players.Add(
            new Player(Context.SenderId, Context.SenderUsername, !canSpeak, Context.SenderFullName));

        Snd(GetGamePlayersText());
        Snd();
        Snd(canSpeak
            ? $"{Context.SenderFullName} has just joined. No one has spoken yet, so they may begin speaking immediately."
            : $"{Context.SenderFullName} has just joined and must wait for the next turn before they can speak.");
    }

    private void CmdResume() {
        if (Context.Game.IsRunning) {
            Snd("Game is already running.");
            return;
        }

        if (!(Context.SendingPlayer?.IsDm ?? false)) {
            Snd(!Context.Game.Players.Any()
                ? "No game has been started in this channel yet."
                : "Only the DM can resume the game.");
            return;
        }

        Context.Game.IsRunning = true;

        Snd("Game resumed.");
    }

    private async Task CmdShowTurn() {
        if (!Context.Game.IsRunning && !Context.Game.Players.Any()) {
            Snd("Game has not been started in this channel yet. To start a game the designated DM should type /start");
            return;
        }

        if (!int.TryParse(Context.MessageText![10..], out var turnReq)) {
            Snd("Use /showturn ### (where ### is the number of the turn)");
            return;
        }

        if (turnReq > Context.Game.TurnNumber) {
            Snd($"There is no turn {turnReq}, game is currently on turn {Context.Game.TurnNumber}");
            return;
        }

        var messages = (await _repository.GetMessages(Context.Game, turnReq)).ToList();

        if (messages.Count == 0) {
            Snd($"No messages found for turn {turnReq}");
            return;
        }

        var disp = messages.Select(m => $"{m.Player.DisplayName}: {m.Text}")
            .Aggregate((x, y) => $"{x}\n\n{y}");
        Snd($"Here are the messages sent on turn {turnReq} (in order):");
        Snd();
        Snd($"{disp}");
    }

    private async Task CmdStartConfirmed() {
        var any = Context.Game.Players.Any();
        if (Context.Game.IsRunning) {
            Snd("Game is already running.");
            return;
        }

        if (any && Context.SendingPlayer != Context.TheDm) {
            Snd("A game is already running and paused. " +
                "Only if the DM ends the game can you start a new one in this channel.");
            return;
        }

        if (any) {
            // Archive existing game
            Context.Game.IsArchived = true;
            Context = Context with {
                Game = await _repository.GetRunningGame(Context.Game.TelegramChannelId, Context.Game.TelegramThreadId),
            };
        }

        Context.Game.IsRunning = true;
        Context.Game.Players.Clear();

        Context.Game.Players.Add(
            new Player(Context.SenderId, Context.SenderUsername, false, Context.SenderFullName) { IsDm = true });

        Snd("Game has started.");
    }

    private void CmdStartNoArgs() {
        if (Context.Game.IsRunning) {
            Snd("Game is already running.");
            return;
        }

        if (Context.Game.Players.Any() && Context.SendingPlayer != Context.TheDm) {
            Snd("A game is already running and paused. " +
                "Only if the DM ends the game can you start a new one in this channel.");
            return;
        }

        var password = new Random().Next(1000, 10000).ToString();
        Snd("This is a serious action, you will start a new game and become its DM. " +
            $"To make sure you really wanted to do that type /start {password}");
        Context.Game.ResetPassword = password;
    }

    private void CmdStartUnconfirmed() {
        Snd($"Wrong confirmation code. If you're really sure, then type /start {Context.Game.ResetPassword}");
    }

    private void CmdStatus() {
        if (!Context.Game.IsRunning && !Context.Game.Players.Any()) {
            Snd("Game has not been started in this channel yet. To start a game the designated DM should type /start");
            return;
        }

        var p = Context.Game.Players.ToLookup(r => r.Played);
        var s = p[true].Select(Mention).DefaultIfEmpty("").Aggregate((x, y) => $"{x}, {y}");
        var ns = p[false].Select(Mention).DefaultIfEmpty("").Aggregate((x, y) => $"{x}, {y}");
        var spoken = p[true].Any()
            ? $"The following players have already spoken: {s}"
            : "No one has spoken yet this turn.";
        var notSpoken = p[true].Any()
            ? $"These players are yet to speak: {ns}"
            : "Everyone may speak.";

        var extra = Context.SendingPlayer is null
            ? "\n\nType /play to join the game."
            : "\n\nType /showturn ### to view messages on a specific turn";

        var paused = Context.Game.IsRunning ? "" : "\n\nGame is paused.";

        Snd(GetGamePlayersText());
        Snd();
        Snd($"We are currently on turn #{Context.Game.TurnNumber}");
        Snd(spoken);
        Snd($"{notSpoken}{extra}{paused}");
    }

    private string GetGamePlayersText() {
        var dmMention = Context.TheDm is not null ? Mention(Context.TheDm) : null;
        var playerMentions = Context.Game.Players.Where(r => !r.IsDm).Select(Mention).DefaultIfEmpty("")
            .Aggregate((x, y) => $"{x}, {y}");
        var pInGame = Context.Game.Players.Any()
            ? $"DM: {dmMention}\nCurrent players: {playerMentions}"
            : "No one has joined the game yet.";
        return pInGame;
    }

    private async Task HandleEdit() {
        if (!Context.Game.IsRunning) {
            return;
        }

        if (Context.SendingPlayer is null) {
            return;
        }

        var originalMessage =
            await _repository.GetOriginalMessage(Context.Game, Context.TgMessage.MessageId, Context.CancellationToken);

        if (originalMessage is null || originalMessage.Text == Context.TgMessage.Text) {
            return;
        }

        if (originalMessage.Turn == Context.Game.TurnNumber) {
            // This edit is allowed, save new message text.
            originalMessage.Text = Context.TgMessage.Text ?? "";
            return;
        }

        await Context.BotClient.DeleteMessageAsync(Context.Game.TelegramChannelId, Context.TgMessage.MessageId,
            Context.CancellationToken);

        Snd($"{Context.SendingPlayer.DisplayName} has edited their message from turn {originalMessage.Turn}, " +
            "the edited message has been deleted.");

        Prv($"You have edited one of your in game messages from turn {originalMessage.Turn}. " +
            "It is not allowed to edit past game messages, your edited message has been deleted. " +
            $"This was your edit: {Context.TgMessage.Text}");
    }

    private async Task HandleGameMessage() {
        if (!Context.Game.IsRunning) {
            // Let the message through, the game is paused or not started
            return;
        }

        var persona = await _repository.GetPerson(Context.SenderId);
        if (Context.SendingPlayer is not null) {
            if (Context.SendingPlayer.Played) {
                try {
                    await Context.BotClient.DeleteMessageAsync(Context.Game.TelegramChannelId,
                        Context.TgMessage.MessageId,
                        Context.CancellationToken);
                }
                catch {
                    /* ignored */
                }

                try {
                    Prv($"You have already spoken this turn (turn #{Context.Game.TurnNumber}). This is your message:");
                    Prv();
                    Prv(Context.MessageText ?? "");
                    persona.InitiatedPrivateChat = true;
                }
                catch (Exception) {
                    Snd(
                        $"{Context.SenderFullName} has tried to send a message out of turn, but they did not initiate " +
                        "a chat with me so I cannot preserve that message in their private chat.\n\n" +
                        $"{Context.SenderFullName}, please send the bot a private message (or alternatively press " +
                        "the 'Start' button in the private chat with the bot)");
                }
            }
            else if (Context.MessageText is not null) {
                await _repository.AddMessage(Context.Game, Context.SendingPlayer, Context.MessageText,
                    Context.TgMessage.MessageId);

                //await botClient.EditMessageTextAsync(chatId, update.Message.MessageId, "zzzzzzzzzzz", cancellationToken: cancellationToken);
                Context.SendingPlayer.Played = true;

                CheckIfEveryoneTalkedAndAdvanceTurn();
            }
        }
    }

    private async Task HandleMemberAdded() {
        if (!Context.Game.IsRunning) {
            return;
        }

        var persona = await _repository.GetPerson(Context.SenderId);

        foreach (var newMember in Context.NewChatMembers!) {
            var fromId2 = newMember.Id;
            var plr2 = Context.Game.Players.SingleOrDefault(r => r.TelegramId == fromId2);
            var fromName2 = new[] { newMember.FirstName, newMember.LastName }.Where(r => r is not null)
                .DefaultIfEmpty("John Doe").Aggregate((x, y) => $"{x} {y}")!;
            if (plr2 is null) {
                var extra = persona.InitiatedPrivateChat
                    ? ""
                    : "\n\nNote: if this is your first time here, please go to the private chat with the bot " +
                      "and send me a message (or you can just click the 'Start' button there)";

                Snd($"Welcome, {fromName2}. Type /help for help, /status for game status, " +
                    "or type /play to join the game.");
                Snd($"{GetGamePlayersText()}{extra}");
            }
        }
    }

    private async Task HandlePrivateMessage() {
        var persona = await _repository.GetPerson(Context.SenderId);
        if (!persona.InitiatedPrivateChat) {
            Snd("Thank you for sending me a message, now I can reply to you privately if you " +
                "accidentally speak out of turn so that your message won't get lost.");
            persona.InitiatedPrivateChat = true;
        }
        else {
            var nGames = persona.Players.Select(r => r.Game).Distinct().Count();
            Snd("Additional messages here have no effect, please chat in one of your game channels. " +
                $"You are currently participating in {nGames} games.");
        }
    }

    private string Mention(Player player) => $"<a href=\"tg://user?id={player.TelegramId}\">{player.DisplayName}</a>";

    private void Prv(string msg = "") {
        if (_anyPrivate) {
            PrivateOutput.AppendLine();
        }

        _anyPrivate = true;
        PrivateOutput.AppendFormat("{0}", msg);
    }

    private void Snd(string msg = "") {
        if (_anyPublic) {
            PublicOutput.AppendLine();
        }

        _anyPublic = true;
        PublicOutput.AppendFormat("{0}", msg);
    }
}
