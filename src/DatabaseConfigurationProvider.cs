
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace ErikNoren.Extensions.Configuration.Database;

public class DatabaseConfigurationProvider<TDbConnection> : ConfigurationProvider, IDisposable where TDbConnection : DbConnection
{
    public DatabaseConfigurationSource<TDbConnection> Source { get; }

    private readonly Timer? _refreshTimer = null;

    public DatabaseConfigurationProvider(DatabaseConfigurationSource<TDbConnection> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Source = source;

        if (Source.RefreshInterval.HasValue)
            _refreshTimer = new Timer(_ => ReadDatabaseSettings(true), null, Timeout.Infinite, Timeout.Infinite);
    }

    public override void Load()
    {
        ReadDatabaseSettings(false);

        if (_refreshTimer != null && Source.RefreshInterval.HasValue)
            _refreshTimer.Change(Source.RefreshInterval.Value, Source.RefreshInterval.Value);
    }

    private void ReadDatabaseSettings(bool isReload)
    {
        if (Source.CreateDbConnection == null || Source.CreateQueryDelegate == null)
            return;

        try
        {
            using var dbConnection = Source.CreateDbConnection();

            var queryCommand = Source.CreateQueryDelegate(dbConnection);

            if (queryCommand == null)
                return;

            using (queryCommand)
            {
                dbConnection.Open();

                using var reader = queryCommand.ExecuteReader();

                var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    try
                    {
                        var setting = Source.GetSettingFromReaderDelegate(reader);

                        //Configuration keys must contain a value
                        if (!string.IsNullOrWhiteSpace(setting.Key))
                            settings[setting.Key] = setting.Value;
                    }
                    catch (Exception readerEx)
                    {
                        System.Diagnostics.Debug.WriteLine(readerEx);
                    }
                }

                reader.Close();

                if (!isReload || !SettingsMatch(Data, settings))
                {
                    Data = settings;

                    if (isReload)
                        OnReload();
                }
            }
        }
        catch (Exception sqlEx)
        {
            System.Diagnostics.Debug.WriteLine(sqlEx);
        }
    }

    private bool SettingsMatch(IDictionary<string, string?> oldSettings, IDictionary<string, string?> newSettings)
    {
        if (oldSettings.Count != newSettings.Count)
            return false;

        return oldSettings
            .OrderBy(s => s.Key)
            .SequenceEqual(newSettings.OrderBy(s => s.Key));
    }

    public void Dispose()
    {
        _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _refreshTimer?.Dispose();
    }
}
