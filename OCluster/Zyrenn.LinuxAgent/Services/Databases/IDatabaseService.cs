using Zyrenn.LinuxAgent.Models.Databases;

namespace Zyrenn.LinuxAgent.Services.Databases;

public interface IDatabaseService
{
    public ValueTask<DatabaseList> GetDatabaseListAsync(CancellationToken ct);
    public ValueTask<DatabaseDetail> GetPostgresDetailAsync(string connectionString, CancellationToken ct);
}