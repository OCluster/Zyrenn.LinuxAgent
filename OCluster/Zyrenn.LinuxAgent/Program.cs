using Serilog;
using Zyrenn.LinuxAgent;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Services.Common;
using Zyrenn.LinuxAgent.Services.Containers;
using Zyrenn.LinuxAgent.Services.Databases;
using Zyrenn.LinuxAgent.Services.Hosts;
using Zyrenn.LinuxAgent.Workers;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine(@"
**********************************************************************************************
*   _____                            _     _                       _                    _    *
*  |__  /   _ _ __ ___ _ __  _ __   | |   (_)_ __  _   ___  __    / \   __ _  ___ _ __ | |_  *
*    / / | | | '__/ _ \ '_ \| '_ \  | |   | | '_ \| | | \ \/ /   / _ \ / _` |/ _ \ '_ \| __| *
*   / /| |_| | | |  __/ | | | | | | | |___| | | | | |_| |>  <   / ___ \ (_| |  __/ | | | |_  *
*  /____\__, |_|  \___|_| |_|_| |_| |_____|_|_| |_|\__,_/_/\_\ /_/   \_\__, |\___|_| |_|\__| *
*       |___/                                                          |___/                 *
**********************************************************************************************");

#region Logger configuration region

builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    //.WriteTo.File("zagent-logs.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

#endregion

ConfigDataHelper.LoadConfiguration(builder.Configuration);

builder.Services.AddHostedService<PeriodicDataProcessor>();
//builder.Services.AddHostedService<AppCommandConsumer>();

builder.Services.AddSingleton<IHostMetricService, HostMetricService>();
builder.Services.AddSingleton<IContainerService, ContainerService>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

var host = builder.Build();
host.Run();