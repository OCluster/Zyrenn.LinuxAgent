using Serilog;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Services.Containers;
using Zyrenn.LinuxAgent.Services.Databases;
using Zyrenn.LinuxAgent.Services.Hosts;
using Zyrenn.LinuxAgent.Workers;

var builder = Host.CreateApplicationBuilder(args);

ConfigDataHelper.LoadConfiguration(builder.Configuration);
builder.Services.AddHostedService<PeriodicDataProcessor>();
builder.Services.AddSingleton<IHostMetricService, HostMetricService>();
builder.Services.AddSingleton<IContainerService, ContainerService>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

#region Logger configuration

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    //.WriteTo.File("zagent-logs.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

#endregion

var host = builder.Build();
host.Run();