using HoneyHouse.Application.Features.Payments;

namespace HoneyHouse.API.BackgroundJobs;

/// <summary>Cancels online orders whose payment window passed, returning their stock.</summary>
public sealed class UnpaidOrderExpiryService(IServiceScopeFactory scopes, IConfiguration config, ILogger<UnpaidOrderExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(config.GetValue("Jobs:UnpaidOrderExpirySeconds", 60));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IPaymentService>().ExpireUnpaidOrdersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unpaid order expiry failed");
            }
        }
    }
}
