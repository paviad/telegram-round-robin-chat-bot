namespace LocalConsoleTest.Data.Models;

internal class Game {
    public int Id { get; set; }
    public string Name { get; set; }
    public int TurnNumber { get; set; }
    public bool IsRunning { get; set; }
    public bool IsArchived { get; set; }
    public long TelegramChannelId { get; set; }
    public int TelegramThreadId { get; set; }
    public string? ResetPassword { get; set; }
    public ICollection<Player> Players { get; set; } = new HashSet<Player>();
}
