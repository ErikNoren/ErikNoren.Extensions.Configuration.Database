# ErikNoren.Extensions.Configuration.Database

This library is a Microsoft.Extensions.Configuration provider that reads settings from a database and makes those values
available via IConfiguration or in a strongly typed class created using Microsoft.Extensions.Options. The provider works
with System.Data.Common.DbConnection so any derived type should be able to use this, Microsoft.Data.SqlClient included.

This provider benefits from the new ASP.NET Core startup in 6.0.0 which allows configuration to be added to and read from
at the same time. This means we can easily read a connection string from a local appsettings.json file that is then used
to configure the DatabaseConfigurationProvider which will then add its new values without a rebuild of configuration data.

For console applications it will still be necessary to build the partial configuration data based on the default providers
before you can access a connection string setting to configure the DatabaseCOnfigurationProvider. Once the provider is
added the configuration will be rebuilt during the builder's build process. It's simpler than it sounds.

Credit for this project goes to Twitch user therealmkb who came by a stream of mine, saw my SqlServerConfigurationProvider
and decided to fork the code, switch to using DbConnection so more providers can be used, and issued a PR. I didn't want
to merge that into the existing project because it would break code for existing users so instead I turned it into its
own project and NuGet package.

## Usage Examples

### Minimum Required Parameters
```csharp
...
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDbConnection<SqlConnection>(config =>
{
    //If the connection string was defined in an appsettings file, environment variable, etc. it can be retrieved here:
    config.CreateDbConnection = () => new SqlConnection(builder.Configuration.GetConnectionString("DemoConnectionString"));
    config.CreateQueryDelegate = sqlConn => new SqlCommand("SELECT SettingKey, SettingValue FROM dbo.Settings", sqlConn);
});
...
```

### Refresh Values
```csharp
...
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDbConnection<SqlConnection>(config =>
{
    //If the connection string was defined in an appsettings file, environment variable, etc. it can be retrieved here:
    config.CreateDbConnection = () => new SqlConnection(builder.Configuration.GetConnectionString("DemoConnectionString"));
    config.CreateQueryDelegate = sqlConn => new SqlCommand("SELECT SettingKey, SettingValue FROM dbo.Settings", sqlConn);

    //Define an interval for the SqlServerConfigurationProvider to reconnect to the database and look for updated settings
    config.RefreshInterval = TimeSpan.FromMinutes(30);
});
...
```


## Database Setup
Since IConfiguration uses string keys and string values a Settings table is very easy to construct. The minimum the table needs
is only 2 columns: one for a setting Key and one for a setting Value. You can of course add additional columns for things like
an IsActive flag to enable the ability to turn settings on and off as needed.

The code samples use a table created with the following SQL:
```sql
CREATE TABLE [dbo].[Settings] (
    [SettingKey]   VARCHAR (2550)  NOT NULL,
    [SettingValue] NVARCHAR (MAX) NULL
);

```

The column used for setting Keys should not allow nulls. All settings must have a non-null, non-empty string. Values can be null.
The type and length of the columns can be whatever length is long enough to accommodate the keys and values. Be sure to create the
key column with enough length to accommodate the flattened structure of setting keys. Meaning nested settings are flattened into a
colon-delimited string so depending on the length of your key names and how deeply they are nested you might end up with quite long
strings in your key column.

For example given the following JSON represenation of nested objects:
```json
{
    "AppSettings": {
        "IsEnabled": true,
        "EmailInfo": {
            "To": "person1@example.com",
            "From": "person2@example.com"
        }
    }
}
```

The flattened key-value pair representation would be:
```
"AppSettings:IsEnabled", "true"
"AppSettings:EmailInfo:To", "person1@example.com"
"AppSettings:EmailInfo:From", "person2@example.com"
```

The keys can get even more complicated when you have arrays of values.
```json
{
    "AppSettings": {
        "IsEnabled": true,
        "EmailInfo": [{
            "To": ["person1@example.com", "person3@example.com", "person4@example.com"],
            "From": "person2@example.com"
        }, {
            "To": ["person5@example.com"],
            "From": "person2@example.com"
        }]
    }
}
```
```
"AppSettings:IsEnabled", "true"
"AppSettings:EmailInfo:0:To:0", "person1@example.com"
"AppSettings:EmailInfo:0:To:1", "person3@example.com"
"AppSettings:EmailInfo:0:To:2", "person4@example.com"
"AppSettings:EmailInfo:0:From", "person2@example.com"
"AppSettings:EmailInfo:1:To:0", "person5@example.com"
"AppSettings:EmailInfo:1:From", "person2@example.com"
```

I was very surprised and happy to see how easy it was to create a new provider and integrate it into my projects.
Read more about [Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration) and [Options](https://docs.microsoft.com/en-us/dotnet/core/extensions/options) on the Microsoft Docs site.


Erik Noren
[@ErikNoren](https://twitter.com/ErikNoren)
