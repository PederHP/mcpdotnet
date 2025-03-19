using McpDotNet;
using AspNetCoreSseServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer().WithTools();
var app = builder.Build();

// Uncomment to use X-API-KEY middleware
//app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/", () => "Hello World!");
app.MapMcpSse();

app.Run();
