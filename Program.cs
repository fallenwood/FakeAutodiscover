using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");
app.UseHealthChecks("/healthz");

app.MapGet(
    "/autodiscover/autodiscover.json",
    async (
      HttpContext context,
      [FromQuery(Name = "Email")] string? email,
      [FromQuery(Name = "Protocol")] string? protocol,
      [FromServices] ILogger<Program> logger) =>
    {
        await AutoDiscoverActiveSyncAsync(context, email, protocol, logger);
    });

app.MapGet(
    "/autodiscover/autodiscover.json/v1.0/{email}",
    async (
      HttpContext context,
      [FromRoute(Name = "email")] string? email,
      [FromQuery(Name = "Protocol")] string? protocol,
      [FromQuery(Name = "RedirectCount")] int redirectCount,
      [FromServices] ILogger<Program> logger) =>
    {
        await AutoDiscoverActiveSyncAsync(context, email, protocol, logger);
    });

await app.RunAsync();

async ValueTask AutoDiscoverAsync(
    HttpContext context,
    string? email,
    string? protocol,
    ILogger<Program> logger)
{
    var socketIp = context.Connection.RemoteIpAddress?.ToString();
    var headerIp = context.Request.Headers["X-Forwarded-For"];

    var ipAddress = headerIp;
    if (string.IsNullOrEmpty(ipAddress))
    {
        ipAddress = socketIp?.ToString();
    }

    logger.LogInformation("Incoming request from '{IPAddress}' with email '{Email}' and protocl '{Protocol}'.", ipAddress, email, protocol);

    context.Response.StatusCode = 200;
    context.Response.Headers.Append("X-Client-IPAddress", ipAddress);

    var task = protocol switch
    {
        "ActiveSync" => AutoDiscoverActiveSyncAsync(context, email, protocol, logger),
        "OutlookGateway" => AutoDiscoverOutlookGatewayAsync(context, email, protocol, logger),
        _ => NotFoundAsync(context, logger),
    };

    await task;
}

async ValueTask AutoDiscoverActiveSyncAsync(
    HttpContext context,
    string? email,
    string? protocol,
    ILogger<Program> logger)
{
    await context.Response.WriteAsJsonAsync<ActiveSyncResponse>(new(protocol: "OutlookGateway", url: "https://outlook.office365.com"));
}


async ValueTask AutoDiscoverOutlookGatewayAsync(
    HttpContext context,
    string? email,
    string? protocol,
    ILogger<Program> logger)
{
    await context.Response.WriteAsJsonAsync<ActiveSyncResponse>(new(protocol: "ActiveSync", url: "https://outlook.office365.com/Microsoft-Server-ActiveSync"));
}

async ValueTask NotFoundAsync(HttpContext context, ILogger<Program> logger)
{
    context.Response.StatusCode = 404;
    await context.Response.WriteAsync("Not Found");
}

public sealed class ActiveSyncResponse(
    string protocol,
    string url)
{
    [JsonPropertyName("Protocol")]
    public string Protocl { get; init; } = protocol;

    [JsonPropertyName("Url")]
    public string Url { get; init; } = url;
}

[JsonSerializable(typeof(ActiveSyncResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}