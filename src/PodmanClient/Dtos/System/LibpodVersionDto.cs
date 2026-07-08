namespace MaksIT.PodmanClientDotNet.Dtos.System;
/// <summary>
/// Deserialized Podman libpod API payload (Libpod Version).
/// </summary>

public sealed class LibpodVersionDto {
  public VersionComponentsDto? Version { get; set; }
  public LibpodPlatformDto? Platform { get; set; }
}

public sealed class LibpodPlatformDto {
  public string? Name { get; set; }
}
/// <summary>
/// Deserialized Podman libpod API payload (Version Components).
/// </summary>

public sealed class VersionComponentsDto {
  public int Major { get; set; }
  public int Minor { get; set; }
  public int Micro { get; set; }
}
