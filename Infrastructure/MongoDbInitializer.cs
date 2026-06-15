namespace SecureMessaging.Api.Infrastructure;

public sealed class MongoDbInitializer(
    MongoDbContext context,
    ILogger<MongoDbInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await context.InitializeAsync(stoppingToken);
                logger.LogInformation("MongoDB indexes initialized successfully.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "MongoDB initialization failed. Retrying in 10 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
