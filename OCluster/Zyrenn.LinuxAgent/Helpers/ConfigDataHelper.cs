using Serilog;
using Zyrenn.LinuxAgent.Models.Common.Config;

namespace Zyrenn.LinuxAgent.Helpers;

/// <summary>
/// This is a helper class for reading config data, which other services can use.
/// This is done to avoid retrieving the config data from the config file every time,
/// or passing it continuously to methods.
/// </summary>
public static class ConfigDataHelper
{
    #region Fields region

    public static string CommunicationKey { get; set; }

    /// <summary>
    /// This property defines the frequency of collecting data, the value will be used in a seconds format.
    /// </summary>
    public static ushort ScrapeInterval { get; set; }

    public static HostConfig HostConfig { get; set; }
    public static List<DatabaseConfig>? DbConfigs { get; set; }

    #endregion

    #region Public methods region

    public static void LoadConfiguration(IConfiguration configuration)
    {
        try
        {
            Log.Information("Loading and setting the configuration.");
            var hostName = configuration["Name"];
            var hostIdentifier = configuration["Identifier"];
            var communicationKey = configuration["CommunicationKey"];

            ArgumentException.ThrowIfNullOrEmpty(communicationKey, nameof(communicationKey));
            ArgumentException.ThrowIfNullOrEmpty(hostIdentifier, nameof(hostIdentifier));


            if (!ushort.TryParse(configuration["ScrapeInterval"], out var scrapeInterval))
            {
                Log.Warning("ScrapeInterval is missing or invalid. Defaulting to 10 seconds.");
                scrapeInterval = 10;
            }

            ScrapeInterval = scrapeInterval;
            CommunicationKey = communicationKey;
            HostConfig = new HostConfig();
            HostConfig.Name = hostName;
            HostConfig.Identifier = hostIdentifier;
            HostConfig.Ips = configuration.GetSection("Ips").Get<string[]>() ?? Array.Empty<string>();
            DbConfigs = configuration.GetSection("DatabaseConnections").Get<List<DatabaseConfig>>() ?? [];

            Log.Information("Configuration loaded successfully for host: {HostName} Tag: [{HostTag}]",
                hostName, hostIdentifier);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load configuration");
            throw;
        }
    }

    #endregion
}