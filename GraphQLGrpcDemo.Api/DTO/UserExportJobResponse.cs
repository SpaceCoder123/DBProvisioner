namespace GraphQLGrpcDemo.Api.DTO;

public class UserExportJobResponse
{
    public required Guid JobId { get; init; }
    public required string Status { get; init; }
    public string? Format { get; init; }
    public string? FileName { get; init; }
    public string? Error { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? DownloadUrl { get; init; }
}
