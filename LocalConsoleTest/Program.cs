// See https://aka.ms/new-console-template for more information

using LocalConsoleTest;
using LocalConsoleTest.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(confBuilder => {
        confBuilder.AddUserSecrets<Program>();
        confBuilder.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((host2, svcCollection) => {
        Configure(svcCollection, host2.Configuration);
    })
    .Build();


var svcp = host.Services;

var receiver = svcp.GetRequiredService<ReceiverService>();

await receiver.ReceiveAsync();

void Configure(IServiceCollection services, IConfiguration conf) {
    var connStr = new SqliteConnectionStringBuilder {
        DataSource = @"c:\myprojects\roundrobinchatbot\localconsoletest\rrbot_data",
    };
    services.AddDbContext<MyDbContext>(opts => opts.UseSqlite(connStr.ConnectionString));
    services.Configure<TelegramOptions>(conf.GetSection("Telegram"));
    services.AddSingleton<MyBot>();
    services.AddSingleton<ReceiverService>();
    services.AddScoped<GameManager>();
}
