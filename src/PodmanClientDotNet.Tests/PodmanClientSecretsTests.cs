using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

namespace MaksIT.PodmanClientDotNet.Tests;

[Trait("Category", "Integration")]
public class PodmanClientSecretsTests {
  private readonly IPodmanClient _client = PodmanClientTestFixture.CreateClient();

  [Fact]
  public async Task SecretLifecycle_Success() {
    var cancellationToken = TestContext.Current.CancellationToken;
    var secretName = $"podman-client-secret-{Guid.NewGuid()}";
    await using var content = new MemoryStream(Encoding.UTF8.GetBytes("podman-client-secret"));

    try {
      var createResult = await _client.CreateSecretAsync(secretName, content, cancellationToken: cancellationToken);
      string? secretId = null;
      PodmanClientTestFixture.AssertSuccess(createResult, value => {
        Assert.NotNull(value);
        secretId = value!.Id;
        Assert.False(string.IsNullOrWhiteSpace(secretId));
      });

      var existsResult = await _client.SecretExistsAsync(secretName, cancellationToken);
      PodmanClientTestFixture.AssertSuccess(existsResult);

      var inspectResult = await _client.InspectSecretAsync(secretName, showSecret: true, cancellationToken: cancellationToken);
      PodmanClientTestFixture.AssertSuccess(inspectResult, value => {
        Assert.NotNull(value);
        Assert.Equal(secretId, value!.Id);
        Assert.Equal(secretName, value.Spec?.Name);
      });

      var listResult = await _client.ListSecretsAsync(cancellationToken: cancellationToken);
      PodmanClientTestFixture.AssertSuccess(listResult, value => Assert.Contains(value!, secret => secret.Name == secretName));

      var deleteResult = await _client.DeleteSecretAsync(secretName, cancellationToken);
      PodmanClientTestFixture.AssertSuccess(deleteResult);

      var existsAfterDelete = await _client.SecretExistsAsync(secretName, cancellationToken);
      PodmanClientTestFixture.AssertFailure(existsAfterDelete);
    }
    finally {
      _ = await _client.DeleteSecretAsync(secretName, cancellationToken);
    }
  }
}

public class PodmanClientApiTests {
  [Fact]
  public async Task HealthCheckContainerAsync_UsesLibpodHealthcheckRoute() {
    var cancellationToken = TestContext.Current.CancellationToken;
    using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Status\":\"healthy\"}", Encoding.UTF8, "application/json"),
    });
    using var httpClient = new HttpClient(handler);
    var client = new PodmanClient(NullLogger<PodmanClient>.Instance, "http://localhost:8080", httpClient);

    var result = await client.HealthCheckContainerAsync("repo/test", cancellationToken);

    Assert.True(result.IsSuccess);
    Assert.Equal("healthy", result.Value?.Status);
    Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    Assert.Equal("http://localhost:8080/v1.41/libpod/containers/repo%2Ftest/healthcheck", handler.LastRequest.RequestUri!.ToString());
  }

  [Fact]
  public async Task CreateSecretAsync_UsesExpectedRouteQueryAndBody() {
    var cancellationToken = TestContext.Current.CancellationToken;
    using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Id\":\"secret-id\"}", Encoding.UTF8, "application/json"),
    });
    using var httpClient = new HttpClient(handler);
    var client = new PodmanClient(NullLogger<PodmanClient>.Instance, "http://localhost:8080", httpClient);
    await using var body = new MemoryStream(Encoding.UTF8.GetBytes("secret-value"));

    var result = await client.CreateSecretAsync(
      "my-secret",
      body,
      driverOptions: new Dictionary<string, string> { ["mode"] = "0600" },
      labels: new Dictionary<string, string> { ["team"] = "api" },
      replace: true,
      cancellationToken: cancellationToken
    );

    Assert.True(result.IsSuccess);
    Assert.Equal("secret-id", result.Value?.Id);
    Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    Assert.Equal("/v1.41/libpod/secrets/create", handler.LastRequest.RequestUri!.AbsolutePath);
    Assert.Equal("my-secret", GetQueryValue(handler.LastRequest.RequestUri, "name"));
    Assert.Equal("file", GetQueryValue(handler.LastRequest.RequestUri, "driver"));
    Assert.Equal("true", GetQueryValue(handler.LastRequest.RequestUri, "replace"));
    Assert.Equal("{\"mode\":\"0600\"}", GetQueryValue(handler.LastRequest.RequestUri, "driveropts"));
    Assert.Equal("{\"team\":\"api\"}", GetQueryValue(handler.LastRequest.RequestUri, "labels"));
    Assert.Equal("secret-value", handler.LastRequestBody);
  }

  [Fact]
  public async Task PullArtifactAsync_UsesExpectedRouteAndRegistryAuthHeader() {
    var cancellationToken = TestContext.Current.CancellationToken;
    using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"ArtifactDigest\":\"sha256:abc\"}", Encoding.UTF8, "application/json"),
    });
    using var httpClient = new HttpClient(handler);
    var client = new PodmanClient(NullLogger<PodmanClient>.Instance, "http://localhost:8080", httpClient);

    var result = await client.PullArtifactAsync(
      "quay.io/example/artifact:latest",
      retry: 2,
      retryDelay: "5s",
      tlsVerify: false,
      authHeader: "encoded-auth",
      cancellationToken: cancellationToken
    );

    Assert.True(result.IsSuccess);
    Assert.Equal("sha256:abc", result.Value?.ArtifactDigest);
    Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    Assert.Equal("/v1.41/libpod/artifacts/pull", handler.LastRequest.RequestUri!.AbsolutePath);
    Assert.Equal("quay.io/example/artifact:latest", GetQueryValue(handler.LastRequest.RequestUri, "name"));
    Assert.Equal("2", GetQueryValue(handler.LastRequest.RequestUri, "retry"));
    Assert.Equal("5s", GetQueryValue(handler.LastRequest.RequestUri, "retryDelay"));
    Assert.Equal("false", GetQueryValue(handler.LastRequest.RequestUri, "tlsVerify"));
    Assert.Equal("encoded-auth", handler.LastRequest.Headers.GetValues("X-Registry-Auth").Single());
  }

  [Fact]
  public async Task RemoveArtifactsAsync_RepeatsArtifactQueryParameters() {
    var cancellationToken = TestContext.Current.CancellationToken;
    using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"ArtifactDigests\":[\"sha256:1\",\"sha256:2\"]}", Encoding.UTF8, "application/json"),
    });
    using var httpClient = new HttpClient(handler);
    var client = new PodmanClient(NullLogger<PodmanClient>.Instance, "http://localhost:8080", httpClient);

    var result = await client.RemoveArtifactsAsync(new[] { "artifact-a", "artifact-b" }, ignore: true, cancellationToken: cancellationToken);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value?.ArtifactDigests);
    Assert.Equal(["sha256:1", "sha256:2"], result.Value!.ArtifactDigests!);
    Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    Assert.Equal("/v1.41/libpod/artifacts/remove", handler.LastRequest.RequestUri!.AbsolutePath);
    Assert.Equal("artifact-a", GetQueryValues(handler.LastRequest.RequestUri, "artifacts")[0]);
    Assert.Equal("artifact-b", GetQueryValues(handler.LastRequest.RequestUri, "artifacts")[1]);
    Assert.Equal("true", GetQueryValue(handler.LastRequest.RequestUri, "ignore"));
  }

  private static string GetQueryValue(Uri uri, string key) => GetQueryValues(uri, key).Single();

  private static List<string> GetQueryValues(Uri uri, string key) =>
    uri.Query.TrimStart('?')
      .Split('&', StringSplitOptions.RemoveEmptyEntries)
      .Select(part => part.Split('=', 2))
      .Where(parts => Uri.UnescapeDataString(parts[0]) == key)
      .Select(parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty)
      .ToList();

  private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler {
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
      LastRequest = request;
      LastRequestBody = request.Content is null
        ? null
        : await request.Content.ReadAsStringAsync(cancellationToken);
      return handler(request);
    }
  }
}
