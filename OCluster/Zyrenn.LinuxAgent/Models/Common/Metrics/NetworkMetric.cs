using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Common.Metrics;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct NetworkMetric
{
    #region Constructors region

    public NetworkMetric(long rxBytes, long txBytes)
    {
        RxBytes = rxBytes;
        TxBytes = txBytes;
    }

    #endregion

    /// <summary>
    /// Total bytes received (RX) through the network interface
    /// </summary>
    /// <example>123456789</example>
    [ProtoMember(1)]
    public long RxBytes { get; }

    /// <summary>
    /// Total bytes transmitted (TX) through the network interface
    /// </summary>
    /// <example>987654321</example>
    [ProtoMember(2)]
    public long TxBytes { get; }
}