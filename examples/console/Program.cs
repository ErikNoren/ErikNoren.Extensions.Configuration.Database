using ErikNoren.Extensions.Configuration.Database;
using ErikNoren.Extensions.Configuration.Database.Console;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateDefaultBuilder(args);

builder
    .ConfigureAppConfiguration((hostBuilder, config) =>
    {
        //This is null because no providers have been built yet.
        //var connectionString = hostBuilder.Configuration.GetConnectionString("DemoConnectionString");

        var partialConfig = config.Build();
        var connectionString = partialConfig.GetConnectionString("DemoConnectionString");

        config.AddDbConnection<SqlConnection>(options =>
        {
            options.CreateDbConnection = () => new SqlConnection(connectionString);
            options.CreateQueryDelegate = sqlConn => new SqlCommand("SELECT SettingKey, SettingValue FROM dbo.Settings", sqlConn);
            options.RefreshInterval = TimeSpan.FromMinutes(30);
        });
    })
    .ConfigureServices((hostBuilder, services) =>
    {
        //By the time ConfigureServices is called the configuration providers have been built.
        services.Configure<Settings>(hostBuilder.Configuration.GetSection("AppSettings"));
    });

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var settings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<Settings>>();

    Console.WriteLine($"{settings.Value.ExampleSetting}");

    Console.WriteLine("Hit [Enter] to exit.");
    Console.ReadLine();
}