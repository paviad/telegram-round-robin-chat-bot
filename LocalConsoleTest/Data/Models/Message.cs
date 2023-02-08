namespace LocalConsoleTest.Data.Models;

internal class Message {
    public int Id { get; set; }
    public int TelegramMessageId { get; set; }
    public int GameId { get; set; }
    public int Turn { get; set; }
    public int PlayerId { get; set; }
    public string Text { get; set; }

    public Game Game { get; set; }
    public Player Player { get; set; }
}
