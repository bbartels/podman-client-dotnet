/// <summary>
/// Podman REST API client contract.
/// </summary>
public interface IPodmanClient
  : IPodmanSystemClient,
    IPodmanContainersClient,
    IPodmanSecretsClient,
    IPodmanArtifactsClient,
    IPodmanImagesClient,
    IPodmanVolumesClient,
    IPodmanNetworksClient,
    IPodmanPodsClient,
    IPodmanExecClient,
    IPodmanBuildClient,
    IPodmanManifestsClient,
    IPodmanGenerateClient { }
