using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unicorn.LiveLogViewer;

var builder = WebApplication.CreateSlimBuilder(args);

// Adds the required dependencies
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddLiveLogViewer( /*optionsBuilder: (provider, options) => {  } */);

builder.Services.AddHostedService<EmitSampleLogsWorker>();

var app = builder.Build();

// Map using the newer IEndpointRouteBuilder
app.MapLiveLogViewer( /*basePath: "/logViewer"*/);
// Or using the older IApplicationBuilder
//app.UseLiveLogViewer(/*basePath: "/logViewer"/*);

app.UseHttpLogging();


app.Run();


class EmitSampleLogsWorker(ILoggerFactory loggerFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        var levels = Enum.GetValues<LogLevel>().Where(l => l != LogLevel.None).ToArray();
        while (!stoppingToken.IsCancellationRequested)
        {
            var level = levels[random.Next(levels.Length)];
            var logger = random.Next(0, 2) switch
            {
                0 => loggerFactory.CreateLogger($"Sample.Logger{random.Next(1, 4)}"),
                1 => loggerFactory.CreateLogger($"Sample.Group{random.Next(1,4)}.Logger{random.Next(1, 4)}"),
                _ => loggerFactory.CreateLogger($"Sample.Group{random.Next(1,4)}.SubGroup{random.Next(1,4)}.Logger{random.Next(1, 4)}"),
            };

            logger.Log(level, "Worker running at: {Time} with log level: {LogLevel}", DateTime.Now, level);
            await Task.Delay(random.Next(100, 2001), stoppingToken);
        }
    }
}