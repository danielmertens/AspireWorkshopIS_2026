using GhostTracker.Transmitter.Interfaces;
using GhostTracker.Transmitter.Models;

namespace GhostTracker.Transmitter;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITransmitter _transmitter;
    private readonly GhostContext _ghostContext;
    private readonly SemaphoreSlim _pauseSemaphore = new(0, 1);
    private bool _isRunning;

    public Worker(ILogger<Worker> logger, ITransmitter transmitter, GhostContext ghostContext)
    {
        _logger = logger;
        _transmitter = transmitter;
        _ghostContext = ghostContext;
    }

    public bool IsRunning => _isRunning;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        await _transmitter.BringOnline(_ghostContext.GhostId);
        _isRunning = true;
        _pauseSemaphore.Release(); // Start in running state
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for permission to run (blocked when paused)
            await _pauseSemaphore.WaitAsync(stoppingToken);
            _pauseSemaphore.Release(); // Immediately release for next iteration
            
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("{time} - Transmitting ghost location.", DateTimeOffset.Now);
            }
            
            await _transmitter.TransmitLocation(_ghostContext.GhostId);
            await Task.Delay(5000, stoppingToken);
        }
    }

    public async Task<bool> StopWorkerAsync()
    {
        if (!_isRunning) return false;

        _logger.LogInformation("Stopping worker...");
        
        // Acquire semaphore to pause the worker
        await _pauseSemaphore.WaitAsync();
        _isRunning = false;
        await _transmitter.TakeOffline(_ghostContext.GhostId);
        
        _logger.LogInformation("Worker stopped.");
        
        // Keep semaphore acquired to maintain paused state
        return true;
    }

    public async Task<bool> StartWorkerAsync()
    {
        if (_isRunning) return false;

        _logger.LogInformation("Starting worker...");
        
        _isRunning = true;
        await _transmitter.BringOnline(_ghostContext.GhostId);
        _pauseSemaphore.Release(); // Resume the worker
        
        _logger.LogInformation("Worker started.");
        
        return true;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            await _transmitter.TakeOffline(_ghostContext.GhostId);
        }
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _pauseSemaphore.Dispose();
        base.Dispose();
    }
}
