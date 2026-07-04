using MaksIT.PodmanClientDotNet.Dtos.Secret;
using MaksIT.Results;

/// <summary>
/// Secret create, list, inspect, exists, and delete endpoints.
/// </summary>
public interface IPodmanSecretsClient {
  Task<Result<SecretCreateResponseDto?>> CreateSecretAsync(
    string name,
    Stream content,
    string driver = "file",
    Dictionary<string, string>? driverOptions = null,
    Dictionary<string, string>? labels = null,
    bool replace = false,
    bool ignore = false,
    CancellationToken cancellationToken = default
  );

  Task<Result<List<SecretListEntryDto>?>> ListSecretsAsync(
    Dictionary<string, string[]>? filters = null,
    CancellationToken cancellationToken = default
  );

  Task<Result<SecretInspectDto?>> InspectSecretAsync(
    string name,
    bool showSecret = false,
    CancellationToken cancellationToken = default
  );

  Task<Result> SecretExistsAsync(string name, CancellationToken cancellationToken = default);
  Task<Result> DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
}
