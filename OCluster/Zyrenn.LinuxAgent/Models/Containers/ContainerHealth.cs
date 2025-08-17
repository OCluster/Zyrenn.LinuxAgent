using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerHealth
{
    #region Constructors region

    public ContainerHealth(string status, ContainerHealthLog[]? healthLog = null)
    {
        Status = status;
        HealthLog = healthLog;
    }

    #endregion

    /// <summary>
    /// Enum: "none", "starting", "healthy", "unhealthy".
    /// "None" Indicates there is no healthcheck.
    /// "Starting" Starting indicates that the container is not yet ready.
    /// "Healthy" Healthy indicates that the container is running correctly.
    /// "Unhealthy" Unhealthy indicates that the container has a problem.
    /// </summary>
    [ProtoMember(1)]
    public string Status { get; }

    [ProtoMember(2)]
    public ContainerHealthLog[]? HealthLog { get; }
}