// See https://aka.ms/new-console-template for more information

using RrBot;
using RrBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(confBuilder => {
        confBuilder.AddJsonFile("appsettings.json");
        confBuilder.AddUserSecrets<Program>();
        confBuilder.AddEnvironmentVariables();
    })
    .ConfigureLogging(opts => opts.AddConsole())
    .ConfigureServices((host2, svcCollection) => {
        Configure(svcCollection, host2.Configuration);
    })
    .Build();

MigrateDb(host);

var svcp = host.Services;

var receiver = svcp.GetRequiredService<ReceiverService>();

await receiver.ReceiveAsync();

void Configure(IServiceCollection services, IConfiguration conf) {
    var connectionString = conf.GetConnectionString("DefaultConnection");
    services.AddDbContext<MyDbContext>(opts => opts.UseSqlServer(connectionString));
    services.Configure<TelegramOptions>(conf.GetSection("Telegram"));
    services.AddSingleton<MyBot>();
    services.AddSingleton<ReceiverService>();
    services.AddScoped<Repository>();
    services.AddScoped<GameManager>();
}

static void MigrateDb(IHost app) {
    using var scope = app.Services.CreateScope();
    var scopeSvcp = scope.ServiceProvider;
    var dc = scopeSvcp.GetRequiredService<MyDbContext>();
    dc.Database.Migrate();
}
