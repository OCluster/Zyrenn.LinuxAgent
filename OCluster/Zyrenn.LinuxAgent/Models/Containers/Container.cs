using System.Runtime.InteropServices;
using ProtoBuf;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Metrics;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Container //obj takes ~ 158 bytes
{
    #region Constructors region

    public Container(
        ContainerDetail detail,
        CpuMetric cpuUsage,
        MemoryMetric memoryUsage,
        DiskMetric diskUsage,
        NetworkMetric networkUsage)
    {
        Detail = detail;
        CpuUsage = cpuUsage;
        MemoryUsage = memoryUsage;
        DiskUsage = diskUsage;
        NetworkUsage = networkUsage;
    }

    #endregion

    [ProtoMember(1)]
    public ContainerDetail Detail { get; }

    [ProtoMember(2)]
    public CpuMetric CpuUsage { get; }

    [ProtoMember(3)]
    public MemoryMetric MemoryUsage { get; }

    [ProtoMember(4)]
    public DiskMetric DiskUsage { get; }

    [ProtoMember(5)]
    public NetworkMetric NetworkUsage { get; }
}