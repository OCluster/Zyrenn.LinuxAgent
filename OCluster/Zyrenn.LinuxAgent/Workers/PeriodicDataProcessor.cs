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
    IDatabaseService databaseService,
    IConfiguration configuration) : BackgroundService
{
    #region Fields region

    private readonly DataPublisher _dataPublisher = new();
    private readonly PeriodicTimer _timeToDelayJob = new(period: TimeSpan.FromSeconds(6));

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
                        name: ConfigDataHelper.HostName,
                        tag: ConfigDataHelper.HostTag,
                        ips: ConfigDataHelper.HostIps,
                        cpuMetric: await hostMetricService.GetCpuUsageAsync().ConfigureAwait(false),
                        memoryMetric: hostMetricService.GetMemoryUsage(),
                        diskMetric: hostMetricService.GetDiskMetrics(),
                        networkMetric: hostMetricService.GetNetworkUsage());
                    //Console.WriteLine(JsonSerializer.Serialize(hostMetric));
                    await _dataPublisher.PublishAsync("host_metric", hostMetric, stoppingToken);

                    //-----Container Data
                    var containers = await containerService.GetContainerListAsync(stoppingToken).ConfigureAwait(false);
                    //Console.WriteLine(JsonSerializer.Serialize(containers));
                    //await _dataPublisher.PublishAsync("host_metric",
                    //    containers, stoppingToken);

                    //-----Db data
                    //  await _dataPublisher.PublishAsync("db_metric",
                    //      await databaseService.GetDatabaseListAsync(stoppingToken), stoppingToken);

                    
                    //todo may be add tag to the db model.
                    var dbData = await databaseService.GetDatabaseListAsync(stoppingToken);
                    Console.WriteLine(JsonSerializer.Serialize(dbData));
                    //;

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await _timeToDelayJob.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Cancellation is requested by a host agent. Stopping data processor.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing data: {ErrorMessage}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                }
            }
        }, stoppingToken);
    }

    #endregion
}