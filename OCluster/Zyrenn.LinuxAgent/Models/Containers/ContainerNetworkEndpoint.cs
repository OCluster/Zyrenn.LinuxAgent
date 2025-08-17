using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerNetworkEndpoint
{
    #region Constructors region

    public ContainerNetworkEndpoint(string networkName, string ipAddress, string gateway, string macAddress = "",
        string ipv6Gateway = "", string globalIPv6Address = "")
    {
        NetworkName = networkName;
        IPAddress = ipAddress;
        Gateway = gateway;
        MacAddress = macAddress;
        IPv6Gateway = ipv6Gateway;
        GlobalIPv6Address = globalIPv6Address;
    }

    #endregion

    [ProtoMember(1)]
    public string NetworkName { get; }
    
    [ProtoMember(2)]
    public string MacAddress { get; }
    
    [ProtoMember(3)]
    public string Gateway { get; }
    
    [ProtoMember(4)]
    public string IPAddress { get; }
    
    [ProtoMember(5)]
    public string IPv6Gateway { get; }
    
    [ProtoMember(6)]
    public string GlobalIPv6Address { get; }
}