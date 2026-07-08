namespace MaksIT.PodmanClientDotNet.Dtos.Container;

/// <summary>Podman returns a JSON array of filesystem change objects.</summary>
public sealed class ContainerChangesDto : List<ContainerChangeDto> {
}

public sealed class ContainerChangeDto {
  public string? Path { get; set; }
  public int Kind { get; set; }
}
