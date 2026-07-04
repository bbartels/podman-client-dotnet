namespace MaksIT.PodmanClientDotNet.Dtos.Secret;
/// <summary>
/// Deserialized Podman libpod API payload (Secret create response).
/// </summary>
public sealed class SecretCreateResponseDto {
  public string? Id { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Secret list entry).
/// </summary>
public sealed class SecretListEntryDto {
  public string? Id { get; set; }
  public string? Name { get; set; }
  public string? Driver { get; set; }
  public string? CreatedAt { get; set; }
  public string? UpdatedAt { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Secret inspect response).
/// </summary>
public sealed class SecretInspectDto {
  public string? Id { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public SecretSpecDto? Spec { get; set; }
  public string? SecretData { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Secret spec).
/// </summary>
public sealed class SecretSpecDto {
  public string? Name { get; set; }
  public SecretDriverDto? Driver { get; set; }
  public Dictionary<string, string>? Labels { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Secret driver spec).
/// </summary>
public sealed class SecretDriverDto {
  public string? Name { get; set; }
  public Dictionary<string, string>? Options { get; set; }
}
