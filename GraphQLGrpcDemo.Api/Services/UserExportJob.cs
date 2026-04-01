namespace GraphQLGrpcDemo.Api.Services;

public class UserExportJob
{
    public Guid JobId { get; init; }
    public required string Format { get; init; }
    public required string Status { get; set; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? Error { get; set; }
}
