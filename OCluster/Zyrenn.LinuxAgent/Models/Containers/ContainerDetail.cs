using System.Runtime.InteropServices;
using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Containers;

[ProtoContract(SkipConstructor = true)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ContainerDetail
{
    #region Constructors region

    public ContainerDetail(string id, string name, string image, ContainerState state,
        IDictionary<string, ContainerNetworkEndpoint> networks)
    {
        Id = id;
        Name = name;
        Image = image;
        State = state;
        Networks = networks;
    }

    #endregion

    [ProtoMember(1)]
    public string Id { get; }
    
    [ProtoMember(2)]
    public string Name { get; }
    
    [ProtoMember(3)]
    public string Image { get; }
    
    [ProtoMember(4)]
    public ContainerState State { get; }
    
    [ProtoMember(5)]
    public IDictionary<string, ContainerNetworkEndpoint> Networks { get; }
}
