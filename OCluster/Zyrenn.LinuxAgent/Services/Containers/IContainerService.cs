using Zyrenn.LinuxAgent.Models.Containers;

namespace Zyrenn.LinuxAgent.Services.Containers;

public interface IContainerService
{
    public ValueTask<Container[]> GetContainerListAsync(CancellationToken cancellationToken);
}