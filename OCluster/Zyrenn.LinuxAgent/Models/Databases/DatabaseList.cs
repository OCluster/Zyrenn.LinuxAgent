using ProtoBuf;

namespace Zyrenn.LinuxAgent.Models.Databases;

[ProtoContract]
public readonly struct DatabaseList
{
    #region Constructors region

    public DatabaseList()
    {
        Timestamp = DateTime.UtcNow;
        Databases = new();
    }

    #endregion

    [ProtoMember(1)]
    public DateTime Timestamp { get; }
    
    [ProtoMember(2)]
    public List<DatabaseDetail> Databases { get; }
}
