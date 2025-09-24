using Zyrenn.LinuxAgent.Helpers;

namespace Zyrenn.LinuxAgent.Workers;

public class AppCommandConsumer : BackgroundService //todo register the service to DI
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    //todo fetch data from the header of nats and execute it the script or command. Below is just a test code.
                    var tag = "test"; //should be real fetched data from the cmd topic.
                    var communicationKey = "something_key";
                    if (tag == ConfigDataHelper.HostConfig.Tag && communicationKey == ConfigDataHelper.CommunicationKey)
                    {
                        //do the back up and all that here
                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        });
    }
}