using System.Net.Http.Headers;
using System.Text.Json;

using MaksIT.PodmanClientDotNet;
using MaksIT.PodmanClientDotNet.Dtos.Secret;
using MaksIT.Results;

public partial class PodmanClient {
  private static string SecretPath(string name) => $"/libpod/secrets/{Uri.EscapeDataString(name)}";

  public Task<Result<SecretCreateResponseDto?>> CreateSecretAsync(
    string name,
    Stream content,
    string driver = "file",
    Dictionary<string, string>? driverOptions = null,
    Dictionary<string, string>? labels = null,
    bool replace = false,
    bool ignore = false,
    CancellationToken cancellationToken = default
  ) {
    var requestContent = new StreamContent(content);
    requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

    return PostLibpodAsync<SecretCreateResponseDto>(
      "/libpod/secrets/create",
      "Create secret",
      PodmanJsonContext.Default.SecretCreateResponseDto,
      requestContent,
      [
        ("name", name),
        ("driver", driver),
        ("driveropts", driverOptions is null ? null : JsonSerializer.Serialize(driverOptions, PodmanJsonContext.Default.DictionaryStringString)),
        ("labels", labels is null ? null : JsonSerializer.Serialize(labels, PodmanJsonContext.Default.DictionaryStringString)),
        ("replace", replace.ToString().ToLowerInvariant()),
        ("ignore", ignore.ToString().ToLowerInvariant()),
      ],
      cancellationToken: cancellationToken
    );
  }

  public Task<Result<List<SecretListEntryDto>?>> ListSecretsAsync(
    Dictionary<string, string[]>? filters = null,
    CancellationToken cancellationToken = default
  ) =>
    GetJsonAsync<List<SecretListEntryDto>>(
      "/libpod/secrets/json",
      "List secrets",
      PodmanJsonContext.Default.ListSecretListEntryDto,
      filters is null ? null : [("filters", JsonSerializer.Serialize(filters, PodmanJsonContext.Default.DictionaryStringStringArray))],
      cancellationToken
    );

  public Task<Result<SecretInspectDto?>> InspectSecretAsync(
    string name,
    bool showSecret = false,
    CancellationToken cancellationToken = default
  ) =>
    GetJsonAsync<SecretInspectDto>(
      $"{SecretPath(name)}/json",
      "Inspect secret",
      PodmanJsonContext.Default.SecretInspectDto,
      [("showsecret", showSecret.ToString().ToLowerInvariant())],
      cancellationToken
    );

  public Task<Result> SecretExistsAsync(string name, CancellationToken cancellationToken = default) =>
    GetWithoutBodyAsync($"{SecretPath(name)}/exists", "Secret exists", cancellationToken: cancellationToken);

  public Task<Result> DeleteSecretAsync(string name, CancellationToken cancellationToken = default) =>
    DeleteWithoutBodyAsync(SecretPath(name), "Delete secret", cancellationToken: cancellationToken);
}
