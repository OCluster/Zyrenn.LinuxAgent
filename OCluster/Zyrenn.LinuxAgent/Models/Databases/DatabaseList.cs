using ProtoBuf;
using Zyrenn.LinuxAgent.Helpers;

namespace Zyrenn.LinuxAgent.Models.Databases;

[ProtoContract]
public readonly struct DatabaseList()
{
    [ProtoMember(1)]
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    [ProtoMember(3)]
    public List<DatabaseDetail> Databases { get; } = [];
}