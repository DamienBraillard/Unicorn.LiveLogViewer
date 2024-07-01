using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "");

app.Run();