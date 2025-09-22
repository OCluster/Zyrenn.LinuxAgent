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

    public static HostConfig HostConfig { get; set; }
    public static List<DatabaseConfig>? DbConfigs { get; set; }

    #endregion

    #region Public methods region

    public static void LoadConfiguration(IConfiguration configuration)
    {
        HostConfig = new HostConfig()
        {
            Name = configuration.GetSection("Host:Name").Value ?? "unknown",
            Tag = configuration.GetSection("Host:Tag").Value ?? "unknown",
            Ips = configuration.GetSection("Host:Ips").Get<string[]>() ?? []
        };
        
        DbConfigs = configuration.GetSection("DatabaseConnections").Get<List<DatabaseConfig>>();
    }


    //todo may be we have to have here also the the config for the database connections and other as well.

    #endregion
}