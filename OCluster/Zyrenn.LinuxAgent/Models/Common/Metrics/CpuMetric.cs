using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Common.Metrics;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct CpuMetric
{
    #region Constructors region

    public CpuMetric(float totalUsage, long iowait, long system, long idle)
    {
        TotalUsage = (byte)Math.Clamp(totalUsage, 0, 100);
        Iowait = iowait;
        System = system;
        Idle = idle;
    }

    #endregion

    /// <summary>
    /// Total cpu_usage.
    /// The value will be represented as a percentage (%).
    /// </summary>
    [ProtoMember(1)]
    public byte TotalUsage { get; }

    /// <summary>
    /// I/O wait time
    /// </summary>
    [ProtoMember(2)]
    public long Iowait { get; }

    [ProtoMember(3)] 
    public long System { get; }

    /// <summary>
    /// Represents the percentage of CPU time that is available or unused.
    /// </summary>
    [ProtoMember(4)]
    public long Idle { get; }
}