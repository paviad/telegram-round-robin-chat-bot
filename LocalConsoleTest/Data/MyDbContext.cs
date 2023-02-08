using LocalConsoleTest.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalConsoleTest.Data;

internal class MyDbContext : DbContext {
    public MyDbContext() { }

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Player>(e => {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();

            e.HasOne(r => r.Game).WithMany(r => r.Players);
        });

        modelBuilder.Entity<Game>(e => {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();

            e.HasMany(r => r.Players).WithOne(r => r.Game);
        });

        modelBuilder.Entity<Person>(e => {
            e.HasKey(r => r.TelegramId);

            e.HasMany(r => r.Players).WithOne().HasForeignKey(r => r.TelegramId);
        });

        modelBuilder.Entity<Message>(e => {
            e.HasKey(r => r.Id);

            e.HasIndex(r => new {
                r.GameId,
                r.Turn,
                r.PlayerId,
            }).IsUnique();

            e.Property(r => r.Id).ValueGeneratedOnAdd();

            e.HasOne(r => r.Game).WithMany();
            e.HasOne(r => r.Player).WithMany();
        });
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Message> Messages { get; set; }
}
