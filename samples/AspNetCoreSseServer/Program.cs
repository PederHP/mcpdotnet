using McpDotNet;
using AspNetCoreSseServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer(options =>
{
    options.Capabilities = new()
    {
        Tools = new()
    };
}).WithTools();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapMcpSse();

app.Run();
