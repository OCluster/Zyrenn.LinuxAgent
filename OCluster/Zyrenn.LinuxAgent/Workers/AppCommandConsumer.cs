using System.Text;
using NATS.Client;
using NATS.Client.JetStream;
using Serilog;
using Zyrenn.LinuxAgent.Helpers;

namespace Zyrenn.LinuxAgent.Workers;

public sealed class AppCommandConsumer : BackgroundService, IAsyncDisposable
{
    #region Fields region

    private IConnection _connection;
    private IJetStreamPullSubscription _subscription;
    private const string Subject = "app_cmd";
    private Task _processingTask;

    #endregion

    #region Methods region

    #region Protected methods region

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = "nats://nats-broker.zyrenn.com";

            _connection = new ConnectionFactory().CreateConnection(opts);
            IJetStream jetStream = _connection.CreateJetStreamContext();
            var pullOpts = PullSubscribeOptions.Builder()
                .WithDurable(ConfigDataHelper.CommunicationKey).Build();

            _subscription = jetStream.PullSubscribe(Subject, pullOpts);
            _processingTask = StartMessageProcessingAsync(stoppingToken);
        });
    }

    #endregion

    #region Private methods region

    private async Task StartMessageProcessingAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting command consumer...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messages = _subscription.Fetch(10, 1); //todo do not hard code it.
                foreach (var msg in messages)
                {
                    await ProcessMessageAsync(msg);
                }
            }
            catch (NATSTimeoutException)
            {
                continue;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching messages: {ex.Message}");
                break;
            }
        }
    }

    private async Task ProcessMessageAsync(Msg msg)
    {
        try
        {
            var commandMessage = Encoding.UTF8.GetString(msg.Data);
            var communicationKey = msg.Header?["communication-key"];
            var tag = msg.Header?["tag"];

            Log.Debug($"[{DateTime.UtcNow:HH:mm:ss} (UTC)] Received command - Tag: {commandMessage}"); //todo enable debug only in DEV mode

            if (tag == ConfigDataHelper.HostConfig.Identifier &&
                communicationKey == ConfigDataHelper.CommunicationKey)
            {
                Log.Debug($"Executing command message for host_tag = {tag}");
                await ExecuteCommandAsync(commandMessage);
                 //todo do not forget to update the state of backup (status )
            }

            msg.Ack();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Message processing error: {ex.Message}");
            msg.Nak(); // Negative acknowledgment - will be redelivered
        }
    }

    private async Task ExecuteCommandAsync(string command)
    {
        ShellCommandExecutor.ExecuteShellCommand(command);
        Console.WriteLine($"Executing backup for tag: {command}");
        await Task.Delay(100);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Wait for processing task to complete
            if (_processingTask != null)
            {
                await _processingTask.ConfigureAwait(false);
            }

            // Dispose resources
            _subscription?.Dispose();
            _connection?.Dispose();

            Console.WriteLine("AppCommand worker disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dispose error: {ex.Message}");
        }
    }

    #endregion

    #endregion
}
