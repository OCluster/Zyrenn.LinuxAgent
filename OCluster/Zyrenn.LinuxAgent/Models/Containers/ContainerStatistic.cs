using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerStatistic
{
    #region Constructors region

    public ContainerStatistic(double cpuPercent, (long Usage, long Limit) memory, double memoryPercent,
        (long Rx, long Tx) network, (long Read, long Write) blockIo)
    {
        CpuPercent = cpuPercent;
        Memory = memory;
        MemoryPercent = memoryPercent;
        Network = network;
        BlockIo = blockIo;
    }

    #endregion

    [ProtoMember(1)]
    public double CpuPercent { get; }
    
    [ProtoMember(2)]
    public (long Usage, long Limit) Memory { get; }
    
    [ProtoMember(3)]
    public double MemoryPercent { get; }
    
    [ProtoMember(4)]
    public (long Rx, long Tx) Network { get; }
    
    [ProtoMember(5)]
    public (long Read, long Write) BlockIo { get; }
}