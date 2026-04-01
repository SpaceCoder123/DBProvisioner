using GraphQLGrpcDemo.Api.Data;
using System.Text;

namespace GraphQLGrpcDemo.Api.Services;

public class UserExportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserExportQueue _queue;
    private readonly UserExportStore _store;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserExportBackgroundService> _logger;
    private bool _activeJobsQueued;

    public UserExportBackgroundService(
        IServiceScopeFactory scopeFactory,
        UserExportQueue queue,
        UserExportStore store,
        IWebHostEnvironment environment,
        ILogger<UserExportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _store = store;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await QueueActiveJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<UserRepository>();
                var store = scope.ServiceProvider.GetRequiredService<UserExportStore>();
                var job = await store.GetAsync(jobId, stoppingToken);

                if (job is null)
                {
                    continue;
                }

                var exportDirectory = System.IO.Path.Combine(_environment.ContentRootPath, "Exports");
                Directory.CreateDirectory(exportDirectory);

                var extension = job.Format == "ndjson" ? "ndjson" : "csv";
                var fileName = $"users-export-{job.JobId:N}.{extension}";
                var filePath = System.IO.Path.Combine(exportDirectory, fileName);

                await store.MarkRunningAsync(job.JobId, DateTime.UtcNow, fileName, filePath, stoppingToken);

                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 65536,
                    useAsync: true);

                await using var writer = new StreamWriter(fileStream, new UTF8Encoding(false));

                if (job.Format == "csv")
                {
                    await writer.WriteLineAsync("Id,FirstName,LastName,Email,PhoneNumber,DateOfBirth,Gender,City,State,IsActive,CreatedAt");
                }

                await foreach (var user in repo.StreamUsersAsync(10000, 120, stoppingToken))
                {
                    if (job.Format == "ndjson")
                    {
                        var line = System.Text.Json.JsonSerializer.Serialize(user);
                        await writer.WriteLineAsync(line);
                    }
                    else
                    {
                        var row = string.Join(",",
                            user.Id,
                            Escape(user.FirstName),
                            Escape(user.LastName),
                            Escape(user.Email),
                            Escape(user.PhoneNumber),
                            user.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty,
                            Escape(user.Gender),
                            Escape(user.City),
                            Escape(user.State),
                            user.IsActive ? "true" : "false",
                            user.CreatedAt.ToString("O"));

                        await writer.WriteLineAsync(row);
                    }
                }

                await writer.FlushAsync(stoppingToken);

                await store.MarkCompletedAsync(job.JobId, DateTime.UtcNow, fileName, filePath, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User export job {JobId} failed.", jobId);

                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<UserExportStore>();
                await store.MarkFailedAsync(jobId, DateTime.UtcNow, ex.Message, stoppingToken);
            }
        }
    }

    private async Task QueueActiveJobsAsync(CancellationToken cancellationToken)
    {
        if (_activeJobsQueued)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<UserExportStore>();
        var jobs = await store.GetActiveJobsAsync(cancellationToken);

        foreach (var job in jobs)
        {
            await _queue.QueueAsync(job.JobId, cancellationToken);
        }

        _activeJobsQueued = true;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
