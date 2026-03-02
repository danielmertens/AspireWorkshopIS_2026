using GhostTracker.GhostManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IGhostQueryService, GhostQueryService>();
builder.Services.AddScoped<IGhostCommandService, GhostCommandService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.MapGet("/ghosts", (IGhostQueryService QueryService) =>
{
    return QueryService.GetAllActiveGhostsSummary();
});

app.MapGet("/ghosts/{id:int}", (int id, IGhostQueryService QueryService) =>
{
    return QueryService.GetGhost(id);
});

app.MapPost("/ghosts/{id:int}/online", (int id, IGhostCommandService CommandService) =>
{
    CommandService.SetGhostStatus(id, true);
});

app.MapPost("/ghosts/{id:int}/offline", (int id, IGhostCommandService CommandService) =>
{
    CommandService.SetGhostStatus(id, false);
});

app.Run();
