using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Unicorn.LiveLogViewer;

var builder = WebApplication.CreateSlimBuilder(args);

// Adds the required dependencies
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddLiveLogViewer( /*optionsBuilder: (provider, options) => {  } */);

var app = builder.Build();

// Map using the newer IEndpointRouteBuilder
app.MapLiveLogViewer( /*basePath: "/logViewer"*/);
// Or using the older IApplicationBuilder
//app.UseLiveLogViewer(/*basePath: "/logViewer"/*);

app.UseHttpLogging();

app.Run();