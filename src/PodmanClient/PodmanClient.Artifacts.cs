using System.Net.Http.Headers;

using MaksIT.PodmanClientDotNet;
using MaksIT.PodmanClientDotNet.Dtos.Artifact;
using MaksIT.Results;

public partial class PodmanClient {
  private static string ArtifactPath(string name) => $"/libpod/artifacts/{Uri.EscapeDataString(name)}";

  public Task<Result<List<ArtifactListEntryDto>?>> ListArtifactsAsync(CancellationToken cancellationToken = default) =>
    GetJsonAsync<List<ArtifactListEntryDto>>(
      "/libpod/artifacts/json",
      "List artifacts",
      PodmanJsonContext.Default.ListArtifactListEntryDto,
      cancellationToken: cancellationToken
    );

  public Task<Result<ArtifactInspectDto?>> InspectArtifactAsync(string name, CancellationToken cancellationToken = default) =>
    GetJsonAsync<ArtifactInspectDto>(
      $"{ArtifactPath(name)}/json",
      "Inspect artifact",
      PodmanJsonContext.Default.ArtifactInspectDto,
      cancellationToken: cancellationToken
    );

  public Task<Result<ArtifactPullResponseDto?>> PullArtifactAsync(
    string name,
    uint? retry = null,
    string? retryDelay = null,
    bool tlsVerify = true,
    string? authHeader = null,
    CancellationToken cancellationToken = default
  ) =>
    PostLibpodAsync<ArtifactPullResponseDto>(
      "/libpod/artifacts/pull",
      "Pull artifact",
      PodmanJsonContext.Default.ArtifactPullResponseDto,
      query: [
        ("name", name),
        ("retry", retry?.ToString()),
        ("retryDelay", retryDelay),
        ("tlsVerify", tlsVerify.ToString().ToLowerInvariant()),
      ],
      registryAuthHeader: authHeader,
      cancellationToken: cancellationToken
    );

  public Task<Result<ArtifactPushResponseDto?>> PushArtifactAsync(
    string name,
    uint? retry = null,
    string? retryDelay = null,
    bool tlsVerify = true,
    string? authHeader = null,
    CancellationToken cancellationToken = default
  ) {
    var query = new List<(string Key, string? Value)> {
      ("retry", retry?.ToString()),
      ("retryDelay", retryDelay),
      ("retrydelay", retryDelay),
      ("tlsVerify", tlsVerify.ToString().ToLowerInvariant()),
    };

    return PostLibpodAsync<ArtifactPushResponseDto>(
      $"{ArtifactPath(name)}/push",
      "Push artifact",
      PodmanJsonContext.Default.ArtifactPushResponseDto,
      query: query,
      registryAuthHeader: authHeader,
      cancellationToken: cancellationToken
    );
  }

  public Task<Result<ArtifactRemoveResponseDto?>> RemoveArtifactAsync(string name, CancellationToken cancellationToken = default) =>
    DeleteJsonAsync<ArtifactRemoveResponseDto>(
      ArtifactPath(name),
      "Remove artifact",
      PodmanJsonContext.Default.ArtifactRemoveResponseDto,
      cancellationToken: cancellationToken
    );

  public Task<Result<ArtifactRemoveResponseDto?>> RemoveArtifactsAsync(
    IEnumerable<string>? artifacts = null,
    bool all = false,
    bool ignore = false,
    CancellationToken cancellationToken = default
  ) {
    var query = new List<(string Key, string? Value)> {
      ("all", all.ToString().ToLowerInvariant()),
      ("ignore", ignore.ToString().ToLowerInvariant()),
    };

    if (artifacts is not null) {
      foreach (var artifact in artifacts)
        query.Add(("artifacts", artifact));
    }

    return DeleteJsonAsync<ArtifactRemoveResponseDto>(
      "/libpod/artifacts/remove",
      "Remove artifacts",
      PodmanJsonContext.Default.ArtifactRemoveResponseDto,
      query,
      cancellationToken
    );
  }

  public Task<Result<ArtifactAddResponseDto?>> AddArtifactAsync(
    string name,
    string fileName,
    Stream content,
    string? fileMimeType = null,
    IEnumerable<string>? annotations = null,
    string? artifactMimeType = null,
    bool append = false,
    bool replace = false,
    CancellationToken cancellationToken = default
  ) {
    var requestContent = new StreamContent(content);
    requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

    return PostLibpodAsync<ArtifactAddResponseDto>(
      "/libpod/artifacts/add",
      "Add artifact",
      PodmanJsonContext.Default.ArtifactAddResponseDto,
      requestContent,
      BuildArtifactAddQuery(name, fileName, fileMimeType, annotations, artifactMimeType, append, replace),
      cancellationToken: cancellationToken
    );
  }

  public Task<Result<ArtifactAddResponseDto?>> AddLocalArtifactAsync(
    string name,
    string path,
    string fileName,
    string? fileMimeType = null,
    IEnumerable<string>? annotations = null,
    string? artifactMimeType = null,
    bool append = false,
    bool replace = false,
    CancellationToken cancellationToken = default
  ) {
    var query = BuildArtifactAddQuery(name, fileName, fileMimeType, annotations, artifactMimeType, append, replace);
    query.Add(("path", path));

    return PostLibpodAsync<ArtifactAddResponseDto>(
      "/libpod/artifacts/local/add",
      "Add local artifact",
      PodmanJsonContext.Default.ArtifactAddResponseDto,
      query: query,
      cancellationToken: cancellationToken
    );
  }

  public Task<Result<Stream?>> ExtractArtifactAsync(
    string name,
    string? title = null,
    string? digest = null,
    bool excludeTitle = false,
    CancellationToken cancellationToken = default
  ) =>
    GetStreamAsync(
      $"{ArtifactPath(name)}/extract",
      "Extract artifact",
      [
        ("title", title),
        ("digest", digest),
        ("excludeTitle", excludeTitle.ToString().ToLowerInvariant()),
      ],
      cancellationToken
    );

  private static List<(string Key, string? Value)> BuildArtifactAddQuery(
    string name,
    string fileName,
    string? fileMimeType,
    IEnumerable<string>? annotations,
    string? artifactMimeType,
    bool append,
    bool replace
  ) {
    var query = new List<(string Key, string? Value)> {
      ("name", name),
      ("fileName", fileName),
      ("fileMIMEType", fileMimeType),
      ("artifactMIMEType", artifactMimeType),
      ("append", append.ToString().ToLowerInvariant()),
      ("replace", replace.ToString().ToLowerInvariant()),
    };

    if (annotations is not null) {
      foreach (var annotation in annotations)
        query.Add(("annotations", annotation));
    }

    return query;
  }
}
