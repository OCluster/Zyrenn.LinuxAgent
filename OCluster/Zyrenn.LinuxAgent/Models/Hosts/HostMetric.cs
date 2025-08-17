using System.Runtime.InteropServices;
using ProtoBuf;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Metrics;

namespace Zyrenn.LinuxAgent.Models.Hosts;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HostMetric
{
    #region Constructors region

    public HostMetric(IConfiguration config, //todo think whether this is good to get the iconfig value every init time
        CpuMetric cpuMetric,
        MemoryMetric memoryMetric,
        DiskMetric diskMetric,
        NetworkMetric networkMetric)
    {
        Name = string.Intern(config.GetSection("Host:Name").Value ?? "unknown");
        Tag =  string.Intern(config.GetSection("Host:Tag").Value ?? "unknown");
        Ips =  config.GetSection("Host:Ips").Get<string[]>() ?? [];
        CpuUsage = cpuMetric;
        MemoryUsage = memoryMetric;
        DiskUsage = diskMetric;
        NetworkUsage = networkMetric;
    }

    #endregion

    /*
    So in general the metadata like hostname, architecture is sent but not all metadata is saved in a clickhouse.
    Ok we will consider where metadata will be saved whether on a clickhouse or a postgres.
    ========== Attention ==========
    The data displayed to the user as a list like hosts and containers, detailed metrics could be shown
    in the details of a host or container. So every new data will be forwarded through kafka or smth.
    */

    #region Metadata properties region

    [ProtoMember(1)]
    public string Name { get; }

    [ProtoMember(2)]
    public string Tag { get; }
    [ProtoMember(3)]
    public string[] Ips { get; }

    [ProtoMember(4)] 
    public DateTime TimeStamp { get; } = DateTime.UtcNow;

    [ProtoMember(5)]
    public string OsType { get; } = "Linux";

    #endregion

    [ProtoMember(6)] 
    public CpuMetric CpuUsage { get; }

    [ProtoMember(7)]
    public MemoryMetric MemoryUsage { get; }

    [ProtoMember(8)]
    public DiskMetric DiskUsage { get; }

    [ProtoMember(9)]
    public NetworkMetric NetworkUsage { get; }
}