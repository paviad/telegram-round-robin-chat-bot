namespace LocalConsoleTest.Data.Models;

internal class Player
{
    public Player() { }

    public Player(long telegramId, string? name, bool played, string displayName)
    {
        Name = name;
        Played = played;
        DisplayName = displayName;
        TelegramId = telegramId;
    }

    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string? Name { get; set; }
    public bool Played { get; set; }
    public bool IsDm { get; set; }
    public string DisplayName { get; set; }

    public Game Game { get; set; }
}
