using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaksIT.PodmanClientDotNet.Dtos.Artifact;
/// <summary>
/// Deserialized Podman libpod API payload (Artifact list entry).
/// </summary>
public sealed class ArtifactListEntryDto {
  [JsonExtensionData]
  public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Artifact inspect response).
/// </summary>
public sealed class ArtifactInspectDto {
  public string? Digest { get; set; }

  [JsonExtensionData]
  public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Artifact add response).
/// </summary>
public sealed class ArtifactAddResponseDto {
  public string? ArtifactDigest { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Artifact pull response).
/// </summary>
public sealed class ArtifactPullResponseDto {
  public string? ArtifactDigest { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Artifact push response).
/// </summary>
public sealed class ArtifactPushResponseDto {
  public string? ArtifactDigest { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Artifact remove response).
/// </summary>
public sealed class ArtifactRemoveResponseDto {
  public string[]? ArtifactDigests { get; set; }
}
