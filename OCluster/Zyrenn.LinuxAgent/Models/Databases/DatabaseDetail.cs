using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Databases;

[ProtoContract]
public struct DatabaseDetail
{
    public string Name { get; set; }
    public string Ip { get; set; }
    public string HostTag { get; set; }
    public long Size { get; set; }
    public int IndexCount { get; set; }
    public int FunctionCount { get; set; }
    public int TriggerCount { get; set; }
    public int ViewCount { get; set; }
    public int MaterializedViewCount { get; set; }
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int ExtensionCount { get; set; }
    public int ProcedureCount { get; set; }
    public int ActiveConnectionCount { get; set; }
    public string Status { get; set; } 
    public string DatabaseType { get; set; }
}