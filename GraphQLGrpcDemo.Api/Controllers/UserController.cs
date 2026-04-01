using Microsoft.AspNetCore.Mvc;
using GraphQLGrpcDemo.Api.Data;
using GraphQLGrpcDemo.Api.DTO;
using GraphQLGrpcDemo.Api.Services;
using System.Text.Json;

namespace GraphQLGrpcDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserRepository _repo;
    private readonly UserExportStore _exportStore;
    private readonly UserExportQueue _exportQueue;

    public UserController(
        UserRepository repo,
        UserExportStore exportStore,
        UserExportQueue exportQueue)
    {
        _repo = repo;
        _exportStore = exportStore;
        _exportQueue = exportQueue;
    }

    [HttpGet]
    public async Task<ActionResult<GetUsersPageResponse>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest("page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > 1000)
        {
            return BadRequest("pageSize must be between 1 and 1000.");
        }

        var totalCount = await _repo.GetUserCountAsync(cancellationToken);
        var items = await _repo.GetUsersPageAsync(page, pageSize, cancellationToken);

        return Ok(new GetUsersPageResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id}/orders")]
    public async Task<IActionResult> GetUserOrders(int id)
    {
        var orders = await _repo.GetOrdersByUserIdAsync(id);
        return Ok(orders);
    }

    [HttpPost("exports")]
    public async Task<ActionResult<UserExportJobResponse>> CreateExport(
        [FromBody] CreateUserExportRequest? request,
        CancellationToken cancellationToken = default)
    {
        var format = string.IsNullOrWhiteSpace(request?.Format)
            ? "csv"
            : request.Format.Trim().ToLowerInvariant();

        if (format is not ("csv" or "ndjson"))
        {
            return BadRequest("format must be either 'csv' or 'ndjson'.");
        }

        var job = await _exportStore.CreateAsync(format, cancellationToken);
        await _exportQueue.QueueAsync(job.JobId, cancellationToken);

        return AcceptedAtAction(nameof(GetExportStatus), new { jobId = job.JobId }, ToResponse(job));
    }

    [HttpGet("exports/{jobId:guid}")]
    public async Task<ActionResult<UserExportJobResponse>> GetExportStatus(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _exportStore.GetAsync(jobId, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    [HttpGet("exports")]
    public async Task<ActionResult<IReadOnlyList<UserExportJobResponse>>> GetExports(
        CancellationToken cancellationToken = default)
    {
        var jobs = await _exportStore.GetAllAsync(cancellationToken);
        var response = jobs
            .Select(ToResponse)
            .ToList();

        return Ok(response);
    }

    [HttpGet("exports/{jobId:guid}/download")]
    public async Task<IActionResult> DownloadExport(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _exportStore.GetAsync(jobId, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        if (!string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(job.FilePath) ||
            !System.IO.File.Exists(job.FilePath))
        {
            return Conflict(new { message = "Export is not ready for download yet." });
        }

        var contentType = string.Equals(job.Format, "ndjson", StringComparison.OrdinalIgnoreCase)
            ? "application/x-ndjson"
            : "text/csv";

        return PhysicalFile(job.FilePath, contentType, job.FileName);
    }

    [HttpGet("stream")]
    [Produces("application/x-ndjson")]
    public async Task StreamUsers(
        [FromQuery] int batchSize = 10000,
        [FromQuery] int commandTimeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 50000)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("batchSize must be between 1 and 50000.", cancellationToken);
            return;
        }

        if (commandTimeoutSeconds is < 30 or > 600)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("commandTimeoutSeconds must be between 30 and 600.", cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";

        await foreach (var user in _repo.StreamUsersAsync(batchSize, commandTimeoutSeconds, cancellationToken))
        {
            await JsonSerializer.SerializeAsync(Response.Body, user, cancellationToken: cancellationToken);
            await Response.WriteAsync("\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private UserExportJobResponse ToResponse(UserExportJob job)
    {
        var downloadUrl = string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            ? Url.Action(nameof(DownloadExport), new { jobId = job.JobId })
            : null;

        return new UserExportJobResponse
        {
            JobId = job.JobId,
            Status = job.Status,
            Format = job.Format,
            FileName = job.FileName,
            Error = job.Error,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            DownloadUrl = downloadUrl
        };
    }
}
