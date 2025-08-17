using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Metrics;

namespace Zyrenn.LinuxAgent.Services.Hosts;

public interface IHostMetricService
{
    public ValueTask<CpuMetric> GetCpuUsageAsync();
    public MemoryMetric GetMemoryUsage();
    public NetworkMetric GetNetworkUsage();
    public DiskMetric GetDiskMetrics();
}