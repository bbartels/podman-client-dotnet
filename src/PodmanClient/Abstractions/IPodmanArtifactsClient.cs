using MaksIT.PodmanClientDotNet.Dtos.Artifact;
using MaksIT.Results;

/// <summary>
/// Artifact inspect, list, pull, push, add, extract, and delete endpoints.
/// </summary>
public interface IPodmanArtifactsClient {
  Task<Result<List<ArtifactListEntryDto>?>> ListArtifactsAsync(CancellationToken cancellationToken = default);
  Task<Result<ArtifactInspectDto?>> InspectArtifactAsync(string name, CancellationToken cancellationToken = default);
  Task<Result<ArtifactPullResponseDto?>> PullArtifactAsync(
    string name,
    uint? retry = null,
    string? retryDelay = null,
    bool tlsVerify = true,
    string? authHeader = null,
    CancellationToken cancellationToken = default
  );

  Task<Result<ArtifactPushResponseDto?>> PushArtifactAsync(
    string name,
    uint? retry = null,
    string? retryDelay = null,
    bool tlsVerify = true,
    string? authHeader = null,
    CancellationToken cancellationToken = default
  );

  Task<Result<ArtifactRemoveResponseDto?>> RemoveArtifactAsync(string name, CancellationToken cancellationToken = default);
  Task<Result<ArtifactRemoveResponseDto?>> RemoveArtifactsAsync(
    IEnumerable<string>? artifacts = null,
    bool all = false,
    bool ignore = false,
    CancellationToken cancellationToken = default
  );

  Task<Result<ArtifactAddResponseDto?>> AddArtifactAsync(
    string name,
    string fileName,
    Stream content,
    string? fileMimeType = null,
    IEnumerable<string>? annotations = null,
    string? artifactMimeType = null,
    bool append = false,
    bool replace = false,
    CancellationToken cancellationToken = default
  );

  Task<Result<ArtifactAddResponseDto?>> AddLocalArtifactAsync(
    string name,
    string path,
    string fileName,
    string? fileMimeType = null,
    IEnumerable<string>? annotations = null,
    string? artifactMimeType = null,
    bool append = false,
    bool replace = false,
    CancellationToken cancellationToken = default
  );

  Task<Result<Stream?>> ExtractArtifactAsync(
    string name,
    string? title = null,
    string? digest = null,
    bool excludeTitle = false,
    CancellationToken cancellationToken = default
  );
}
