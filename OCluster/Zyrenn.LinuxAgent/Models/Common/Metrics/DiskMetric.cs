using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Common.Metrics;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DiskMetric
{
    #region Constructors region

    public DiskMetric(float total, double reads, double writes)
    {
        Total = (byte)Math.Clamp(total, 0, 100);
        Reads = Math.Round(reads, 2);
        Writes = Math.Round(writes, 2);
    }

    #endregion

    /// <summary>
    /// Total disk_usage represented as a percentage (%).
    /// </summary>
    [ProtoMember(1)]
    public byte Total { get; }

    /// <summary>
    /// Read bytes per second.
    /// </summary>
    [ProtoMember(2)]
    public double Reads { get; }

    /// <summary>
    /// Write bytes per second.
    /// </summary>
    [ProtoMember(3)]
    public double Writes { get; }
}