using Zyrenn.LinuxAgent.Models.Databases;

namespace Zyrenn.LinuxAgent.Models.Common;

public struct DatabaseConfig
{
    public DatabaseType Type { get; set; }
    public string Connection { get; set; }
}