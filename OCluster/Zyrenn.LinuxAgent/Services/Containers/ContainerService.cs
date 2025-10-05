using System.Buffers;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Helpers.Extensions;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Metrics;
using Zyrenn.LinuxAgent.Models.Containers;
using ContainerState = Zyrenn.LinuxAgent.Models.Containers.ContainerState;

namespace Zyrenn.LinuxAgent.Services.Containers;

public class ContainerService(IConfiguration configuration) : IContainerService
{
    #region Private fields region

    private readonly IConfiguration _configuration = configuration;
    
    //todo may be the count of threads will be retrieved form the configuration
    private readonly SemaphoreSlim _containerSemaphore = new(5, 5);
    private readonly SemaphoreSlim _shellCommandSemaphore = new(5, 5);

    #endregion

    #region Public methods region

    public async ValueTask<Container[]> GetContainerListAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<Container> containerList = new();
            var client = new DockerClientConfiguration().CreateClient();
            var containers =
                await client.Containers.ListContainersAsync(new ContainersListParameters { All = true },
                    cancellationToken);

            if (!containers.Any()) return null;
            
            var tasks = containers.Select(async container =>
            {
                await _containerSemaphore.WaitAsync(cancellationToken);
                try
                {
                    await _shellCommandSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var statsTask = Task.Run(() =>
                        {
                            var stats = ShellCommandExecutor.ExecuteShellCommand(
                                $"docker stats {container.ID} --no-stream");
                            return ParseDockerStats(stats);
                        }, cancellationToken);


                        var inspectTask = client.Containers.InspectContainerAsync(container.ID, cancellationToken);
                        await Task.WhenAll(statsTask, inspectTask);

                        var inspectResponse = await inspectTask;
                        var parsedStats = await statsTask;

                        var containerDetail = new ContainerDetail(
                            id: inspectResponse.ID,
                            name: inspectResponse.Name,
                            image: inspectResponse.Config.Image,
                            state: new ContainerState
                            (
                                status: inspectResponse.State.Status,
                                error: inspectResponse.State.Error,
                                exitCode: inspectResponse.State.ExitCode,
                                startedAt: DateTime.Parse(inspectResponse.State.StartedAt),
                                finishedAt: DateTime.Parse(inspectResponse.State.FinishedAt),
                                health: inspectResponse.State.Health == null
                                    ? null
                                    : new ContainerHealth
                                    (
                                        status: inspectResponse.State.Health.Status,
                                        healthLog: inspectResponse.State.Health.Log == null
                                            ? null
                                            : inspectResponse.State.Health.Log.Select(x =>
                                                new ContainerHealthLog(x.ExitCode, x.Output)).ToArray()
                                    )
                            ),
                            networks: inspectResponse.NetworkSettings.Networks.ToDictionary(
                                pair => pair.Key,
                                pair => new ContainerNetworkEndpoint
                                (
                                    networkName: pair.Key,
                                    ipAddress: pair.Value.IPAddress,
                                    gateway: pair.Value.Gateway,
                                    macAddress: pair.Value.MacAddress,
                                    ipv6Gateway: pair.Value.IPv6Gateway,
                                    globalIPv6Address: pair.Value.GlobalIPv6Address
                                )
                            )
                        );

                        var cpuMetric = new CpuMetric((float)parsedStats.CpuPercent, 0, 0, 0);
                        var diskMetric = new DiskMetric(0, parsedStats.BlockIo.Read, parsedStats.BlockIo.Write);
                        var networkMetric = new NetworkMetric(parsedStats.Network.Rx, parsedStats.Network.Tx);

                        containerList.Add(new Container
                        (
                            detail: containerDetail,
                            cpuUsage: cpuMetric,
                            memoryUsage: default, //todo why is memory usage not filled look at this.
                            diskUsage: diskMetric,
                            networkUsage: networkMetric
                        ));
                    }
                    finally
                    {
                        _shellCommandSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to process container {ContainerId}. Error: {ErrorMessage}",
                        container.ID, ex.Message);
                }
                finally
                {
                    _containerSemaphore.Release();
                }
            });
            await Task.WhenAll(tasks);

            return ArrayPool<Container>.Shared.RentReturn(containerList.Count,
                buffer => { containerList.CopyTo(buffer); });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to list Docker containers. Error: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    #endregion

    #region Private methods region

    private static Task<ContainerStatistic> ParseDockerStats(string rawOutput)
    {   
        //The header line should be skipped
        var lines = rawOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) throw new FormatException("No stats data found");

        // Regex to split columns while handling spaces in container names
        var regex = new Regex(@"\s{2,}"); // Split on 2+ whitespace
        var columns = regex.Split(lines[1].Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();

        if (columns.Length < 8)
            throw new FormatException($"Unexpected column count: {columns.Length} when parsing docker stats.");

        return Task.FromResult(new ContainerStatistic
        (
            cpuPercent: SystemMetricHelper.ParsePercentage(columns[2]),
            memory: SystemMetricHelper.ParseMemory(columns[3]),
            memoryPercent: SystemMetricHelper.ParsePercentage(columns[4]),
            network: SystemMetricHelper.ParseNetwork(columns[5]),
            blockIo: SystemMetricHelper.ParseBlockIO(columns[6])
        ));
    }

    #endregion
}

#region Comments region

/*public async ValueTask<Container[]> ListContainersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = new DockerClientConfiguration().CreateClient();
            var containers =
                await client.Containers.ListContainersAsync(new ContainersListParameters { All = true },
                    cancellationToken);

            foreach (var container in containers)
            {
                try
                {
                    var stats = ShellCommandExecutor.ExecuteShellCommand(
                        $"docker stats {container.ID} --no-stream");
                    var parsedStats = ParseDockerStats(stats);
                    var inspectResponse =
                        await client.Containers.InspectContainerAsync(container.ID, cancellationToken);

                    var containerDetail = new ContainerDetail(
                        id: inspectResponse.ID,
                        name: inspectResponse.Name,
                        image: inspectResponse.Config.Image,
                        state: new ContainerState
                        (
                            status: inspectResponse.State.Status,
                            error: inspectResponse.State.Error,
                            exitCode: inspectResponse.State.ExitCode,
                            startedAt: DateTime.Parse(inspectResponse.State.StartedAt),
                            finishedAt: DateTime.Parse(inspectResponse.State.FinishedAt),
                            health: inspectResponse.State.Health == null
                                ? null
                                : new ContainerHealth
                                (
                                    status: inspectResponse.State.Health.Status,
                                    healthLog: inspectResponse.State.Health.Log == null
                                        ? null
                                        : inspectResponse.State.Health.Log.Select(x =>
                                            new ContainerHealthLog(x.ExitCode, x.Output)).ToArray()
                                )
                        ),
                        networks: inspectResponse.NetworkSettings.Networks.ToDictionary(
                            pair => pair.Key,
                            pair => new ContainerNetworkEndpoint
                            (
                                networkName: pair.Key,
                                ipAddress: pair.Value.IPAddress,
                                gateway: pair.Value.Gateway,
                                macAddress: pair.Value.MacAddress,
                                ipv6Gateway: pair.Value.IPv6Gateway,
                                globalIPv6Address: pair.Value.GlobalIPv6Address
                            )
                        )
                    );

                    var cpuMetric = new CpuMetric((float)parsedStats.CpuPercent, 0, 0, 0);
                    var diskMetric = new DiskMetric(0, parsedStats.BlockIo.Read, parsedStats.BlockIo.Write);
                    var networkMetric = new NetworkMetric(parsedStats.Network.Rx, parsedStats.Network.Tx);

                    _containerList.Add(new Container
                    (
                        detail: containerDetail,
                        cpuUsage: cpuMetric,
                        memoryUsage: default,
                        diskUsage: diskMetric,
                        networkUsage: networkMetric
                    ));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to process container {ContainerId}. Error: {ErrorMessage}",
                        container.ID, ex.Message);
                }
            }

            return _containerList.ToArray();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to list Docker containers. Error: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            _containerList.Clear();
        }
    }*/
/* Memory issue
 Large Object Heap: code that allocates a lot of memory in LOH
   Allocated object type: Byte[]
   Last observation: 5/11/2025 4:58 PM Zyrenn.LinuxAgent
     Allocated size: 643.0 MB

   at SharedArrayPool<Container>.Rent(int)
   at DataPublisher+<PublishAsync>d__4<HostMetric>.MoveNext() in /home/abu-usman/RiderProjects/OCluster.Zyrenn/Zyrenn.LinuxAgent/Services/Common/DataPublisher.cs:line 49 column 13
   at AsyncMethodBuilderCore.Start(ref )
   at DataPublisher.PublishAsync(String, , CancellationToken)
   at PeriodicDataProcessor+<>c__DisplayClass5_0+<<ExecuteAsync>b__0>d.MoveNext() in /home/abu-usman/RiderProjects/OCluster.Zyrenn/Zyrenn.LinuxAgent/Workers/PeriodicDataProcessor.cs:line 50 column 21
   at ExecutionContext.RunInternal(ExecutionContext, ContextCallback, Object)
   at AsyncTaskMethodBuilder+AsyncStateMachineBox<VoidTaskResult,StartupHook+<ReceiveDeltas>d__3>.MoveNext(Thread)
   at AwaitTaskContinuation.RunOrScheduleAction(IAsyncStateMachineBox, bool)
   at Task.RunContinuations(Object)
   at AsyncValueTaskMethodBuilder<__Canon>.SetResult()
   at ContainerService.ListContainersAsync(CancellationToken) in /home/abu-usman/RiderProjects/OCluster.Zyrenn/Zyrenn.LinuxAgent/Services/Containers/ContainerService.cs:line 132 column 5
   at ExecutionContext.RunInternal(ExecutionContext, ContextCallback, Object)
   at AsyncTaskMethodBuilder+AsyncStateMachineBox<VoidTaskResult,StartupHook+<ReceiveDeltas>d__3>.MoveNext(Thread)
   at AwaitTaskContinuation.RunOrScheduleAction(IAsyncStateMachineBox, bool)
   at Task.RunContinuations(Object)
   at Parallel+ForEachAsyncState<__Canon>.Complete()
   at Parallel+<>c__53+<<ForEachAsync>b__53_0>d<__Canon>.MoveNext()
   at ExecutionContext.RunInternal(ExecutionContext, ContextCallback, Object)
   at AsyncTaskMethodBuilder+AsyncStateMachineBox<VoidTaskResult,StartupHook+<ReceiveDeltas>d__3>.MoveNext(Thread)
   at AwaitTaskContinuation.RunOrScheduleAction(IAsyncStateMachineBox, bool)
   at Task.RunContinuations(Object)
   at AsyncValueTaskMethodBuilder.SetResult()
   at ContainerService+<>c__DisplayClass1_0+<<ListContainersAsync>b__0>d.MoveNext() in /home/abu-usman/RiderProjects/OCluster.Zyrenn/Zyrenn.LinuxAgent/Services/Containers/ContainerService.cs:line 115 column 13
   at ExecutionContext.RunInternal(ExecutionContext, ContextCallback, Object)
   at AwaitTaskContinuation.RunOrScheduleAction(IAsyncStateMachineBox, bool)
   at Task.RunContinuations(Object)
   at Task+WhenAllPromise.Invoke(Task)
   at Task.RunContinuations(Object)
   at Task.FinishSlow(bool)
   at Task.ExecuteWithThreadLocal(ref Task, Thread)
   at ThreadPoolWorkQueue.Dispatch()

 */

#endregion