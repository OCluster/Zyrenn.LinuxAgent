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
    public static HostConfig HostConfig { get; set; }
    public static List<DatabaseConfig>? DbConfigs { get; set; }

    #endregion

    #region Public methods region

    /*public static void LoadConfiguration(IConfiguration configuration)
    {
        CommunicationKey = configuration.GetSection("CommunicationKey").Value ?? "unknown";
        HostConfig = new HostConfig()
        {
            Name = configuration.GetSection("Name").Value ?? "unknown",
            Tag = configuration.GetSection("Tag").Value ?? "unknown",
            Ips = configuration.GetSection("Ips").Get<string[]>() ?? []
        };

        DbConfigs = configuration.GetSection("DatabaseConnections").Get<List<DatabaseConfig>>();
    }*/

    public static void LoadConfiguration(IConfiguration configuration)
    {
        var hostName = configuration.GetSection("Name").Value;
        var hostTag = configuration.GetSection("Tag").Value;
        var communicationKey = configuration.GetSection("CommunicationKey").Value;

        ArgumentException.ThrowIfNullOrEmpty(communicationKey,
            paramName: "Communication key is required and cannot be empty.");
        ArgumentException.ThrowIfNullOrEmpty(hostTag,
            paramName: "Host tag is required and cannot be empty."); //todo consider may be if the tag is empty, we can use the host name instead.

        CommunicationKey = communicationKey;
        HostConfig = new HostConfig()
        {
            Name = hostName,
            Tag = hostTag,
            Ips = configuration.GetSection("Ips").Get<string[]>() ?? []
        };

        DbConfigs = configuration.GetSection("DatabaseConnections").Get<List<DatabaseConfig>>();
    }

    #endregion
}