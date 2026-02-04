using GhostTracker.Transmitter;
using GhostTracker.Transmitter.ApiClients;
using GhostTracker.Transmitter.Interfaces;
using GhostTracker.Transmitter.Models;
using GhostTracker.Transmitter.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Worker>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<Worker>());
builder.Services.AddSingleton<ITransmitter, FakeTransmitter>();

builder.Services.AddHttpClient<GhostManagerApiClient>(static client => client.BaseAddress = new("http://localhost:5095"));
builder.Services.AddHttpClient<PathFinderApiClient>(static client => client.BaseAddress = new("http://localhost:5059"));

builder.Services.AddSingleton((ctx) => new GhostContext { GhostId = 1});

var app = builder.Build();
    
app.MapPost("/offline", async ([FromServices] Worker worker) =>
{
    var success = await worker.StopWorkerAsync();
    return success 
        ? Results.Ok(new { status = "offline" })
        : Results.BadRequest(new { error = "Worker is not running" });
});

app.MapPost("/online", async ([FromServices] Worker worker) =>
{
    var success = await worker.StartWorkerAsync();
    return success
        ? Results.Ok(new { status = "online" })
        : Results.BadRequest(new { error = "Worker is already running" });
});

app.MapPost("/teleport", ([FromServices] ITransmitter transmitter) =>
{
    transmitter.Teleport();
    return Results.Ok(new { message = "Teleported to new location" });
});

app.Run();
