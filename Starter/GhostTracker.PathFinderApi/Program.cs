using GhostTracker.PathFinderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPathFinderService, PathFinderService>();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.MapPost("/ghosts/find", (int[] ghostIds, IPathFinderService pathFinderService) =>
{
    return pathFinderService.GetGhostLocations(ghostIds);
});

app.MapPost("/ghosts/register-location", (LocationDto dto, IPathFinderService pathFinderService) =>
{
    pathFinderService.AddGhostCoordinate(dto.ghostId, dto.Coordinate, dto.Heading);
});

app.Run();

public class LocationDto
{
    public int ghostId { get; set; }
    public Coordinate Coordinate { get; set; }
    public Heading Heading { get; set; }
}