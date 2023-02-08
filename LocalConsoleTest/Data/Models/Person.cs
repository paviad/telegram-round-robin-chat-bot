namespace LocalConsoleTest.Data.Models;

internal class Person {
    public long TelegramId { get; set; }
    public bool InitiatedPrivateChat { get; set; }
    public ICollection<Player> Players { get; set; } = new HashSet<Player>();
}
