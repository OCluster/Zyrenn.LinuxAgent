using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Databases;

[ProtoContract]
public struct DatabaseDetail
{
    [ProtoMember(1)]
    public string Name { get; set; }
    
    [ProtoMember(2)]
    public string Ip { get; set; }
    
    [ProtoMember(3)]
    public long Size { get; set; }
    
    [ProtoMember(4)]
    public int IndexCount { get; set; }
    
    [ProtoMember(5)]
    public int FunctionCount { get; set; }
    
    [ProtoMember(6)]
    public int TriggerCount { get; set; }
    
    [ProtoMember(7)]
    public int ViewCount { get; set; }
    
    [ProtoMember(8)]
    public int MaterializedViewCount { get; set; }
    
    [ProtoMember(9)]
    public int UserCount { get; set; }
    
    [ProtoMember(10)]
    public int RoleCount { get; set; }
    
    [ProtoMember(11)]
    public int ExtensionCount { get; set; }
    
    [ProtoMember(12)]
    public int ProcedureCount { get; set; }
    
    [ProtoMember(13)]
    public int ActiveConnectionCount { get; set; }
    
    [ProtoMember(14)]
    public string Status { get; set; } 
    
    [ProtoMember(15)]
    public string DatabaseType { get; set; }
}
