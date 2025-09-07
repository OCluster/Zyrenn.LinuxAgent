namespace Zyrenn.LinuxAgent.Helpers;

/// <summary>
/// This is a helper class for reading config data, which other services can use.
/// This is done to avoid retrieving the config data from the config file every time,
/// or passing it continuously to methods.
/// </summary>
public class ConfigDataHelper
{
    #region Fields region

    public static string HostName { get; private set; } = "unknown";
    public static string HostTag { get; private set; } = "unknown";
    public static string[] HostIps { get; private set; } = Array.Empty<string>();

    #endregion

    #region Public methods region

    public static void LoadConfiguration(IConfiguration configuration)
    {
        HostName = configuration.GetSection("Host:Name").Value ?? "unknown";
        HostTag = configuration.GetSection("Host:Tag").Value ?? "unknown";
        HostIps = configuration.GetSection("Host:Ips").Get<string[]>() ?? Array.Empty<string>();
    }

    #endregion
}