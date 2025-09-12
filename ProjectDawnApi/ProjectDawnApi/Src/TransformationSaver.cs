using ProjectDawnApi;
using Microsoft.EntityFrameworkCore;

public class TransformationSaver : BackgroundService
{
    private const int SaveIntervalSeconds = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransformationSaver> _logger;

    public TransformationSaver(IServiceScopeFactory scopeFactory, ILogger<TransformationSaver> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SERVER] TransformationSaver started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(SaveIntervalSeconds), stoppingToken);

                if (TransformationQueue.Queue.IsEmpty)
                    continue;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProjectDawnDbContext>();

                foreach (var kvp in TransformationQueue.Queue)
                {
                    var (farmId, playerId) = kvp.Key;
                    var transformation = kvp.Value;

                    var visitor = await db.FarmVisitors
                        .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId, stoppingToken);

                    if (visitor != null)
                    {
                        visitor.Transformation = transformation;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"[SERVER] Persisted {TransformationQueue.Queue.Count} latest transformations to DB.");

                // Clear after save
                TransformationQueue.Queue.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SERVER] Error in TransformationSaver loop.");
            }
        }
    }
}
