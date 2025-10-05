using System.Globalization;
using Serilog;
using Serilog.Events;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Helpers.Extensions;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Metrics;
using SpanLineEnumerator = Zyrenn.LinuxAgent.Helpers.Extensions.SpanLineEnumerator;

namespace Zyrenn.LinuxAgent.Services.Hosts;

public class HostMetricService : IHostMetricService
{
    #region Fields region

    private static List<DiskStats> _previousStats;
    private static DateTime _previousCollectionTime = DateTime.MinValue;

    #endregion

    #region Methods region

    #region Public methods region

    public ValueTask<CpuMetric> GetCpuUsageAsync()
    {
        try
        {
            int index = 0;
            var values = new long[15];
            var line = File.ReadAllLines("/proc/stat")[0].AsSpan();

            foreach (var part in line.SplitFast(' '))
            {
                if (long.TryParse(part, out long val))
                {
                    if (index >= values.Length) break;
                    values[index++] = val;
                }
            }

            //=========== Attention ===========
            //  values[0]: user time
            //  values[1]: nice time
            //  values[2]: system time
            //  values[3]: idle time
            //  values[4]: iowait time
            //=========== Attention ===========
            long idle = values[3] + values[4];
            long total = 0;
            foreach (long val in values) total += val;

            return ValueTask.FromResult(new CpuMetric
            (
                totalUsage: SystemMetricHelper.CalculateUsage(idle, total),
                iowait: values[4],
                system: values[2],
                idle: idle
            ));
        }
        catch (Exception ex)
        {
            Log.Write(LogEventLevel.Error, messageTemplate: "Failed to retrieve CPU metrics: {ErrorMessage}",
                propertyValue: ex.Message);
            return ValueTask.FromResult(default(CpuMetric));
        }
    }

    public MemoryMetric GetMemoryUsage()
    {
        try
        {
            var output = ShellCommandExecutor.ExecuteShellCommand("top -bn1 | grep 'MiB Mem'");
            var outputSpan = output.AsSpan();
            MemoryMetric memory = default;

            var lineEnumerator = new SpanLineEnumerator(outputSpan);
            while (lineEnumerator.MoveNext())
            {
                var line = lineEnumerator.Current;
                if (line.Contains("MiB Mem", StringComparison.OrdinalIgnoreCase))
                {
                    memory = ParseTopMemoryLine(line);
                    break;
                }
            }

            return memory;
        }
        catch (Exception ex)
        {
            Log.Write(LogEventLevel.Error, ex,
                "Failed to retrieve memory metrics: {ErrorMessage}", ex.Message);
            return default;
        }
    }

    public NetworkMetric GetNetworkUsage()
    {
        try
        {
            var output = ShellCommandExecutor.ExecuteShellCommand(
                $"ip -s -j link show {SystemMetricHelper.GetActivePhysicalInterface()} " +
                $"| jq -r '.[0].stats64.rx.bytes, .[0].stats64.tx.bytes'"
            );

            var parts = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return new NetworkMetric(
                    rxBytes: long.Parse(parts[0]),
                    txBytes: long.Parse(parts[1]));
            }

            Log.Warning("Incomplete network metrics data: Received {PartCount} data points instead of expected 2",
                parts.Length);
            return default;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve network metrics: {ErrorMessage}", ex.Message);
            return default;
        }
    }

    public DiskMetric GetDiskMetrics()
    {
        try
        {
            var currentStats = GetDiskStats();
            var currentTime = DateTime.UtcNow;

            if (_previousCollectionTime == DateTime.MinValue)
            {
                _previousStats = currentStats;
                _previousCollectionTime = currentTime;
                return new DiskMetric(0f, 0f, 0f);
            }

            var timeElapsedSeconds = (currentTime - _previousCollectionTime).TotalSeconds;
            if (timeElapsedSeconds <= 0)
                return new DiskMetric(0f, 0f, 0f);

            // Calculating total deltas across all disks
            long currentReadBytes = currentStats.Sum(d => d.ReadBytes);
            long currentWriteBytes = currentStats.Sum(d => d.WriteBytes);
            long currentIoTimeMs = currentStats.Sum(d => d.IoTimeMs);

            long previousReadBytes = _previousStats.Sum(d => d.ReadBytes);
            long previousWriteBytes = _previousStats.Sum(d => d.WriteBytes);
            long previousIoTimeMs = _previousStats.Sum(d => d.IoTimeMs);

            // Throughput (bytes/sec)
            var readRate = (currentReadBytes - previousReadBytes) / timeElapsedSeconds;
            var writeRate = (currentWriteBytes - previousWriteBytes) / timeElapsedSeconds;

            // Utilization (%)
            float utilization = Math.Min(100f,
                (currentIoTimeMs - previousIoTimeMs) / (float)(timeElapsedSeconds * 1000) * 100f);

            _previousStats = currentStats;
            _previousCollectionTime = currentTime;

            return new DiskMetric(utilization, readRate, writeRate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve disk metrics: {ErrorMessage}", ex.Message);
            return new DiskMetric(0f, 0f, 0f);
        }
    }

    #endregion

    #region Private methods region

    public static MemoryMetric ParseTopMemoryLine(ReadOnlySpan<char> line)
    {
        //================== Attention ==================
        //      Example line:
        // "MiB Mem: 38628.4 total, 1234.5 free, 5678.9 used, 31715.0 buff/cache"
        //================== Attention ===================
        long total = 0, free = 0, used = 0, cache = 0;

        // Find the colon and get the data portion
        int colonIndex = line.IndexOf(':');
        if (colonIndex == -1)
        {
            return default;
        }

        var dataSpan = line.Slice(colonIndex + 1).Trim();

        // Manually split by commas and process each segment
        while (!dataSpan.IsEmpty)
        {
            // Find the next comma or end of a span
            int commaIndex = dataSpan.IndexOf(',');
            ReadOnlySpan<char> segment = commaIndex == -1
                ? dataSpan
                : dataSpan.Slice(0, commaIndex).Trim();

            if (!segment.IsEmpty)
            {
                int lastSpace = segment.LastIndexOf(' ');
                if (lastSpace != -1)
                {
                    var valuePart = segment.Slice(0, lastSpace).Trim();
                    var labelPart = segment.Slice(lastSpace + 1).Trim();

                    if (double.TryParse(valuePart, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out double valueMiB))
                    {
                        long valueBytes = (long)(valueMiB * 1024 * 1024);

                        if (labelPart.Equals("total", StringComparison.OrdinalIgnoreCase))
                            total = valueBytes;
                        else if (labelPart.Equals("free", StringComparison.OrdinalIgnoreCase))
                            free = valueBytes;
                        else if (labelPart.Equals("used", StringComparison.OrdinalIgnoreCase))
                            used = valueBytes;
                        else if (labelPart.Equals("buff/cache", StringComparison.OrdinalIgnoreCase))
                            cache = valueBytes;
                    }
                }
            }

            // Processing to the next segment
            dataSpan = commaIndex == -1 ? default : dataSpan.Slice(commaIndex + 1).Trim();
        }

        return new MemoryMetric(total, free, cache, used);
    }

    private static List<DiskStats> GetDiskStats()
    {
        var disks = new List<DiskStats>();
        foreach (var line in File.ReadLines("/proc/diskstats"))
        {
            var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 14 || int.Parse(parts[1]) != 0)
            {
                continue;
            }

            //=========== Attention ===========
            //  parts[2]: Disk name
            //  parts[5]: Read bytes
            //  parts[9]: Write bytes
            //  parts[12]: I/O time (ms)
            //=========== Attention ===========
            disks.Add(new DiskStats(
                DiskName: parts[2],
                ReadBytes: long.Parse(parts[5]) * 512,
                WriteBytes: long.Parse(parts[9]) * 512,
                IoTimeMs: long.Parse(parts[12])
            ));
        }

        return disks;
    }

    public record struct DiskStats(
        string DiskName,
        long ReadBytes,
        long WriteBytes,
        long IoTimeMs
    );

#endregion

    #endregion
}