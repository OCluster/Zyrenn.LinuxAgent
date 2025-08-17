using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Common.Metrics;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MemoryMetric
{
    #region Constructors region

    public MemoryMetric(long total, long free, long cache, long used)
    {
        Total = total;
        Cache = cache;
        Used = used;
        Free = free;
        TotalUsage = Total > 0 ? (byte)((Used * 100) / Total) : (byte)0;
    }

    #endregion

    /// <summary>
    /// The total available memory.
    /// </summary>
    [ProtoMember(1)]
    public long Total { get; set; }

    /// <summary>
    /// Total memory_usage.
    /// The value will be represented as a percentage (%).
    /// </summary>
    [ProtoMember(2)]
    public byte TotalUsage { get; set; }

    [ProtoMember(3)] 
    public long Cache { get; set; }

    [ProtoMember(4)]
    public long Used { get; set; }

    [ProtoMember(5)]
    public long Free { get; set; }
}