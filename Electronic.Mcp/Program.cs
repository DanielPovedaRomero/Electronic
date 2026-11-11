using Electronic.Application;
using Electronic.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory   // always finds appsettings.json next to the .dll
});

// MCP uses the console (stdout) to talk to Claude, so all logs must go to stderr
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// The same services your API uses
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()   // talk through the console
    .WithToolsFromAssembly();     // find every class marked [McpServerToolType]

await builder.Build().RunAsync();