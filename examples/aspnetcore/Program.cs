using ErikNoren.Extensions.Configuration.Database;
using ErikNoren.Extensions.Configuration.Database.AspNetCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDbConnection<SqlConnection>(options =>
{
    options.CreateDbConnection = () => new SqlConnection(builder.Configuration.GetConnectionString("DemoConnectionString"));
    options.CreateQueryDelegate = conn => new SqlCommand("SELECT SettingKey, SettingValue FROM dbo.Settings", conn);
    options.RefreshInterval = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<Settings>(builder.Configuration.GetSection("AppSettings"));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

//If a database source is configured with a SettingKey of "AppSettings:ExampleSetting" it should be retrieved and shown here
app.MapGet("/TestSetting", (IOptionsSnapshot<Settings> settingsAccessor) => $"{settingsAccessor.Value.ExampleSetting}");

app.Run();
