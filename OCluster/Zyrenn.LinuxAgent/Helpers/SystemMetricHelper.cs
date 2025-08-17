using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Zyrenn.LinuxAgent.Helpers;

public static class SystemMetricHelper
{
    #region Fields region

    private static long _prevIdle;
    private static long _prevTotal;

    #endregion

    #region Public methods region

    #region Helpers for host service

    /// <summary>
    /// Identifies the primary active physical network interface
    /// </summary>
    /// <returns>
    /// Name of the first active physical network interface.
    /// Defaults to "eth0" if no active interface is found.
    /// </returns>
    /// <remarks>
    /// Prioritizes Ethernet interfaces first, then wireless interfaces.
    /// Only considers interfaces that are operational and have non-virtual hardware.
    /// </remarks>
    public static string GetActivePhysicalInterface()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            !n.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase) &&
                            !n.Name.Contains("virtual", StringComparison.OrdinalIgnoreCase) &&
                            (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                             n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .ThenByDescending(n => n.Speed)
                .ToList();

            return interfaces.FirstOrDefault()?.Name ?? "eth0";
        }
        catch
        {
            return "eth0";
        }
    }

    /// <summary>
    /// Calculates CPU usage percentage from idle and total time values
    /// </summary>
    /// <param name="currentIdle">Current idle time</param>
    /// <param name="currentTotal">Current total time</param>
    /// <returns>CPU usage percentage (0-100)</returns>
    /// <remarks>
    /// Uses differential calculation between two measurements to determine
    /// actual CPU utilization percentage
    /// </remarks>
    public static float CalculateUsage(long currentIdle, long currentTotal)
    {
        long idleDelta = currentIdle - _prevIdle;
        long totalDelta = currentTotal - _prevTotal;

        _prevIdle = currentIdle;
        _prevTotal = currentTotal;

        if (totalDelta <= 0 || idleDelta <= 0) return 0f;

        float usage = (1f - (float)idleDelta / totalDelta) * 100f;
        return Math.Clamp(usage, 0f, 100f);
    }

    #endregion

    #region Helpers for container service

    public static double ParsePercentage(string input) =>
        double.Parse(input.TrimEnd('%'));

    public static (long Usage, long Limit) ParseMemory(string input)
    {
        if (string.IsNullOrEmpty(input)) return (0, 0);

        var parts = input.Split(new[] { " / " }, StringSplitOptions.None);
        if (parts.Length != 2) return (0, 0);

        return (ParseBytes(parts[0]), ParseBytes(parts[1]));
    }

    public static (long Rx, long Tx) ParseNetwork(string input)
    {
        if (string.IsNullOrEmpty(input)) return (0, 0);

        var parts = input.Split(new[] { " / " }, StringSplitOptions.None);
        if (parts.Length != 2) return (0, 0);

        return (ParseBytes(parts[0]), ParseBytes(parts[1]));
    }

    public static (long Read, long Write) ParseBlockIO(string input)
    {
        var parts = input.Split(new[] { " / " }, StringSplitOptions.None);
        return (ParseBytes(parts[0]), ParseBytes(parts[1]));
    }

    public static long ParseBytes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;

        try
        {
            var match = Regex.Match(input, @"(\d+\.?\d*)(\w+)");
            if (!match.Success) return 0;

            var value = double.Parse(match.Groups[1].Value);
            return match.Groups[2].Value.ToUpper() switch
            {
                "KIB" or "KB" => (long)(value * 1024),
                "MIB" or "MB" => (long)(value * 1024 * 1024),
                "GIB" or "GB" => (long)(value * 1024 * 1024 * 1024),
                "TIB" or "TB" => (long)(value * 1024 * 1024 * 1024 * 1024),
                "B" or _ => (long)value
            };
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #endregion   
}