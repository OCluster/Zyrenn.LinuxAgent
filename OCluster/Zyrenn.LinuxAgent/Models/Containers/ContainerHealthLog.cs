using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerHealthLog
{
    #region Constructors region

    public ContainerHealthLog(long logExitCode, string logOutput)
    {
        LogExitCode = logExitCode;
        LogOutput = logOutput;
    }

    #endregion

    /// <summary>
    /// Examples: 0, 1, 2.
    /// 0 healthy 
    /// 1 unhealthy
    /// 2 reserved (considered unhealthy)
    /// other values: error running probe 
    /// </summary>
    [ProtoMember(1)]
    public long LogExitCode { get; }

    [ProtoMember(2)]
    public string LogOutput { get; }
}