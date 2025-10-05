using System.Runtime.InteropServices;
using System.Text.Json;
using NATS.Client.JetStream;
using Serilog;
using Serilog.Events;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Hosts;
using Zyrenn.LinuxAgent.Services.Common;
using Zyrenn.LinuxAgent.Services.Containers;
using Zyrenn.LinuxAgent.Services.Databases;
using Zyrenn.LinuxAgent.Services.Hosts;

namespace Zyrenn.LinuxAgent.Workers;

/// <summary>
/// Handles periodic data collection and processing tasks for a host agent.
/// This includes gathering host metrics, container data, and publishing the collected
/// information to a broker.
/// </summary>
public class PeriodicDataProcessor(
    IHostMetricService hostMetricService,
    IContainerService containerService,
    IDatabaseService databaseService) : BackgroundService
{
    #region Fields region

    private readonly DataPublisher _dataPublisher = new();
    private readonly PeriodicTimer _timeToDelayJob = new(
        period: TimeSpan.FromSeconds(ConfigDataHelper.ScrapeInterval));

    #endregion

    #region Protected methods region

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            Log.Information("Host agent's data processor is running. About to process data.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var hostMetric = new HostMetric(
                        name: ConfigDataHelper.HostConfig.Name,
                        identifier: ConfigDataHelper.HostConfig.Identifier,
                        ips: ConfigDataHelper.HostConfig.Ips,
                        cpuMetric: await hostMetricService.GetCpuUsageAsync().ConfigureAwait(false),
                        memoryMetric: hostMetricService.GetMemoryUsage(),
                        diskMetric: hostMetricService.GetDiskMetrics(),
                        networkMetric: hostMetricService.GetNetworkUsage());
                    //Console.WriteLine(JsonSerializer.Serialize(hostMetric));
                    await _dataPublisher.PublishAsync("host_metric", hostMetric, stoppingToken);

                    //-----Container Data
                    var containers = await containerService.GetContainerListAsync(stoppingToken).ConfigureAwait(false); 
                    //Console.WriteLine(JsonSerializer.Serialize(containers));
                    if (containers != null && containers.Length != 0)
                    {
                        await _dataPublisher.PublishAsync("container_metric",
                            containers, stoppingToken);
                    }
                    
                    //-----Db data
                    var dbs = await databaseService.GetDatabaseListAsync(stoppingToken);
                    if (dbs.Databases.Count != 0)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(dbs));
                        await _dataPublisher.PublishAsync("db_metric",
                            dbs, stoppingToken);
                    }
                    
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await _timeToDelayJob.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Cancellation is requested by a host agent. Stopping agent's worker.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing data: {ErrorMessage}", ex.Message);
                    return;
                }
            }
        }, stoppingToken);
    }

    #endregion
}