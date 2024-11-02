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

async ValueTask AutoDiscoverActiveSyncAsync(
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
    await context.Response.WriteAsJsonAsync<ActiveSyncResponse>(new());
}


public sealed class ActiveSyncResponse(
    string protocol = "ActiveSync",
    string url = "https://outlook.office365.com/Microsoft-Server-ActiveSync")
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
