using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerState
{
    #region Constructors region

    public ContainerState(string status, string error, long exitCode, DateTime startedAt,
        DateTime finishedAt, ContainerHealth? health = null)
    {
        Status = status;
        Error = error;
        ExitCode = exitCode;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        Health = health;
    }

    #endregion

    /// <summary>
    /// The state of the container.
    /// Examples: "created", "running", "paused", "restarting", "removing", "exited", "dead".
    /// </summary>
    [ProtoMember(1)]
    public string Status { get; }

    [ProtoMember(2)]
    public string Error { get; }
    
    [ProtoMember(3)]
    public long ExitCode { get; }
    
    [ProtoMember(4)]
    public DateTime StartedAt { get; }
    
    [ProtoMember(5)]
    public DateTime FinishedAt { get; }
    
    [ProtoMember(6)]
    public ContainerHealth? Health { get; }
}

