using Dapper;
using Microsoft.Data.SqlClient;

namespace GraphQLGrpcDemo.Api.Services;

public class UserExportStore
{
    private const int DefaultCommandTimeoutSeconds = 120;
    private readonly IConfiguration _config;

    public UserExportStore(IConfiguration config)
    {
        _config = config;
    }

    private SqlConnection GetConnection()
        => new(_config.GetConnectionString("DefaultConnection"));

    public async Task<UserExportJob> CreateAsync(string format, CancellationToken cancellationToken = default)
    {
        var job = new UserExportJob
        {
            JobId = Guid.NewGuid(),
            Format = format,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        const string sql = @"
            INSERT INTO UserExportJobs
            (
                JobId,
                Format,
                Status,
                CreatedAtUtc
            )
            VALUES
            (
                @JobId,
                @Format,
                @Status,
                @CreatedAtUtc
            );";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            job,
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(command);
        return job;
    }

    public async Task<UserExportJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                JobId,
                Format,
                Status,
                CreatedAtUtc,
                StartedAtUtc,
                CompletedAtUtc,
                FilePath,
                FileName,
                Error
            FROM UserExportJobs
            WHERE JobId = @JobId;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            new { JobId = jobId },
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return await conn.QuerySingleOrDefaultAsync<UserExportJob>(command);
    }

    public async Task<IReadOnlyList<UserExportJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                JobId,
                Format,
                Status,
                CreatedAtUtc,
                StartedAtUtc,
                CompletedAtUtc,
                FilePath,
                FileName,
                Error
            FROM UserExportJobs
            ORDER BY CreatedAtUtc DESC;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return (await conn.QueryAsync<UserExportJob>(command)).AsList();
    }

    public async Task<IReadOnlyList<UserExportJob>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                JobId,
                Format,
                Status,
                CreatedAtUtc,
                StartedAtUtc,
                CompletedAtUtc,
                FilePath,
                FileName,
                Error
            FROM UserExportJobs
            WHERE Status IN ('Pending', 'Running')
            ORDER BY CreatedAtUtc ASC;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return (await conn.QueryAsync<UserExportJob>(command)).AsList();
    }

    public async Task MarkRunningAsync(
        Guid jobId,
        DateTime startedAtUtc,
        string? fileName,
        string? filePath,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE UserExportJobs
            SET
                Status = 'Running',
                StartedAtUtc = @StartedAtUtc,
                FileName = @FileName,
                FilePath = @FilePath,
                Error = NULL
            WHERE JobId = @JobId;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                JobId = jobId,
                StartedAtUtc = startedAtUtc,
                FileName = fileName,
                FilePath = filePath
            },
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(command);
    }

    public async Task MarkCompletedAsync(
        Guid jobId,
        DateTime completedAtUtc,
        string fileName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE UserExportJobs
            SET
                Status = 'Completed',
                CompletedAtUtc = @CompletedAtUtc,
                FileName = @FileName,
                FilePath = @FilePath,
                Error = NULL
            WHERE JobId = @JobId;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                JobId = jobId,
                CompletedAtUtc = completedAtUtc,
                FileName = fileName,
                FilePath = filePath
            },
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(command);
    }

    public async Task MarkFailedAsync(
        Guid jobId,
        DateTime completedAtUtc,
        string error,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE UserExportJobs
            SET
                Status = 'Failed',
                CompletedAtUtc = @CompletedAtUtc,
                Error = @Error
            WHERE JobId = @JobId;";

        using var conn = GetConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                JobId = jobId,
                CompletedAtUtc = completedAtUtc,
                Error = error
            },
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(command);
    }
}
