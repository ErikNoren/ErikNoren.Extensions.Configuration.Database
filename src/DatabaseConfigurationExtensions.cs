
using Microsoft.Extensions.Configuration;
using System;
using System.Data.Common;

namespace ErikNoren.Extensions.Configuration.Database;

public static class DatabaseConfigurationExtensions
{
    public static IConfigurationBuilder AddDbConnection<T>(this IConfigurationBuilder builder, Action<DatabaseConfigurationSource<T>>? configurationSource) where T : DbConnection
        => builder.Add(configurationSource);
}
