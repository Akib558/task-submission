using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MetricsService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapGet("/api/metrics", async (MetricsService metricsService) =>
{
    var metrics = await metricsService.GetMetrics();
    return Results.Ok(metrics);
});

app.Run();

public class ServiceResponse
{
    public int ServiceId { get; set; }
    public int Uptime { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int Duration { get; set; }
}

public class Microservice
{
    private int _uptime = 0;
    private int _errorCount = 0;
    private int _successCount = 0;
    private readonly CancellationToken _token;
    private readonly Task _simulationTask;
    private readonly Random _random = new();
    private int serviceId = 0;

    public Microservice(CancellationToken token, int serviceId)
    {
        _token = token;
        _simulationTask = Task.Run(SimulateAsync, _token);
        this.serviceId = serviceId;
    }

    private async Task SimulateAsync()
    {
        while (!_token.IsCancellationRequested)
        {
            try
            {
                Interlocked.Increment(ref _uptime);

                if (_random.NextDouble() < 0.2)
                {
                    Interlocked.Increment(ref _errorCount);
                }
                else
                {
                    Interlocked.Increment(ref _successCount);
                }

                await Task.Delay(_random.Next(300, 700), _token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task<ServiceResponse> GetMetrics()
    {
        await Task.Delay(_random.Next(100, 1000));

        return new ServiceResponse
        {
            ServiceId = serviceId,
            Uptime = Interlocked.CompareExchange(ref _uptime, 0, 0),
            ErrorCount = Interlocked.CompareExchange(ref _errorCount, 0, 0),
            SuccessCount = Interlocked.CompareExchange(ref _successCount, 0, 0)
        };
    }

}


public class MetricsService
{
    private readonly List<Microservice> _microservices;
    private readonly CancellationTokenSource _cts;

    public MetricsService()
    {
        _cts = new CancellationTokenSource();
        _microservices = Enumerable.Range(0, 20)
            .Select(ind => new Microservice(_cts.Token, ind))
            .ToList();
    }

    public async Task<object> GetMetrics()
    {
        var metrics = new ConcurrentBag<object>();

        await Parallel.ForEachAsync(_microservices, async (service, _) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var response = await service.GetMetrics();
            sw.Stop();
            
            response.Duration = (int)sw.ElapsedMilliseconds;
            
            metrics.Add(response);
        });

        var sortedMetrics = metrics
            .OfType<dynamic>() 
            .OrderBy(m => (int)m.ServiceId) 
            .ToList();

        return sortedMetrics;

    }
}