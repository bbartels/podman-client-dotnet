using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using MaksIT.PodmanClientDotNet.Models;

namespace MaksIT.PodmanClientDotNet.Tests;

public sealed class PodmanClientRegressionTests {
  [Fact]
  public async Task CreateContainerAsync_Should_Serialize_SnakeCase_And_OciMounts() {
    string? requestBody = null;

    var client = CreateClient((request, cancellationToken) => {
      requestBody = request.Content is null
        ? null
        : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) {
        Content = new StringContent("{\"Id\":\"abc\",\"Warnings\":[]}", Encoding.UTF8, "application/json")
      });
    });

    var result = await client.CreateContainerAsync(
      name: "c",
      image: "alpine",
      capDrop: ["ALL"],
      noNewPrivileges: true,
      ociRuntime: "runc",
      sdnotifyMode: "ignore",
      containerCreateCommand: ["podman", "create"],
      idMappings: new IDMappingOptions {
        AutoUserNs = true
      },
      mounts: [
        new Mount {
          Type = "tmpfs",
          Target = "/x",
          TmpfsOptions = new TmpfsOptions {
            SizeBytes = 1024,
            Options = ["exec"]
          }
        }
      ]
    );

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.NotNull(requestBody);

    using var json = JsonDocument.Parse(requestBody!);
    var root = json.RootElement;

    Assert.Equal("c", root.GetProperty("name").GetString());
    Assert.Equal("alpine", root.GetProperty("image").GetString());
    Assert.Equal("runc", root.GetProperty("oci_runtime").GetString());
    Assert.True(root.GetProperty("no_new_privileges").GetBoolean());
    Assert.Equal("ALL", root.GetProperty("cap_drop")[0].GetString());
    Assert.Equal("ignore", root.GetProperty("sdnotifyMode").GetString());
    Assert.Equal("podman", root.GetProperty("containerCreateCommand")[0].GetString());
    Assert.True(root.GetProperty("idmappings").GetProperty("AutoUserNs").GetBoolean());

    var mount = root.GetProperty("mounts")[0];
    Assert.Equal("tmpfs", mount.GetProperty("type").GetString());
    Assert.Equal("/x", mount.GetProperty("destination").GetString());
    Assert.Equal(string.Empty, mount.GetProperty("source").GetString());

    var options = mount.GetProperty("options").EnumerateArray().Select(x => x.GetString()).ToArray();
    Assert.Contains("rw", options);
    Assert.Contains("exec", options);
    Assert.Contains("size=1024", options);
  }

  [Fact]
  public async Task PingAsync_Should_Succeed_On_Text_Response() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("OK", Encoding.UTF8, "text/plain")
    }));

    var result = await client.PingAsync(TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.True(result.Value?.Ping);
  }

  [Fact]
  public async Task GetVersionAsync_Should_Read_Platform_Object() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Version\":{\"Major\":5,\"Minor\":1,\"Micro\":0},\"Platform\":{\"Name\":\"linux/amd64/ubuntu-24.04\"}}", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetVersionAsync(TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal("linux/amd64/ubuntu-24.04", result.Value?.Platform?.Name);
  }

  [Fact]
  public async Task InspectContainerAsync_Should_Read_Rfc3339_Created() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Id\":\"x\",\"Name\":\"x\",\"Created\":\"2026-07-08T11:18:26.416458796+01:00\"}", Encoding.UTF8, "application/json")
    }));

    var result = await client.InspectContainerAsync("x", TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal("2026-07-08T11:18:26.4164587+01:00", result.Value?.Created.ToString("O"));
  }

  [Fact]
  public async Task ListContainersAsync_Should_Read_Command_Array_And_Rfc3339_Created() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("[{\"Id\":\"x\",\"Names\":[\"x\"],\"Command\":[\"sleep\",\"300\"],\"Created\":\"2026-07-08T11:18:26.416458796+01:00\"}]", Encoding.UTF8, "application/json")
    }));

    var result = await client.ListContainersAsync(cancellationToken: TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!);
    Assert.Equal(["sleep", "300"], result.Value![0].Command);
    Assert.Equal("2026-07-08T11:18:26.4164587+01:00", result.Value![0].Created.ToString("O"));
  }

  [Fact]
  public async Task DefaultClient_Should_Use_V4_0_0_Api_Version() {
    Uri? requestUri = null;
    var client = CreateClient((request, _) => {
      requestUri = request.RequestUri;
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new StringContent("[]", Encoding.UTF8, "application/json")
      });
    });

    var result = await client.ListNetworksAsync(TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.NotNull(requestUri);
    Assert.Equal("/v4.0.0/libpod/networks/json", requestUri!.AbsolutePath);
  }

  [Fact]
  public async Task GetInfoAsync_Should_Read_Distribution_Object() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Host\":{\"Distribution\":{\"Distribution\":\"ubuntu\",\"Version\":\"24.04\",\"Codename\":\"noble\"}}}", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetInfoAsync(TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal("ubuntu", result.Value?.Host?.Distribution?.Distribution);
    Assert.Equal("24.04", result.Value?.Host?.Distribution?.Version);
    Assert.Equal("noble", result.Value?.Host?.Distribution?.Codename);
  }

  [Fact]
  public async Task ListImagesAsync_Should_Read_RepoTag_Arrays() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("[{\"Id\":\"img\",\"RepoTags\":[\"example/alpine:latest\"],\"RepoDigests\":[\"example/alpine@sha256:123\"]}]", Encoding.UTF8, "application/json")
    }));

    var result = await client.ListImagesAsync(cancellationToken: TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal(["example/alpine:latest"], result.Value![0].RepoTags);
    Assert.Equal(["example/alpine@sha256:123"], result.Value![0].RepoDigests);
  }

  [Fact]
  public async Task GetContainerStatsAsync_Should_Read_Network_Map() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Id\":\"c\",\"Networks\":{\"eth0\":{\"RxBytes\":516,\"TxBytes\":42}}}", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetContainerStatsAsync("c", cancellationToken: TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal((ulong)516, result.Value?.Networks?["eth0"].RxBytes);
    Assert.Equal((ulong)42, result.Value?.Networks?["eth0"].TxBytes);
  }

  [Fact]
  public async Task GetContainersStatsAsync_Should_Read_Stats_List_Response() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Error\":null,\"Stats\":[{\"Id\":\"c\",\"Name\":\"demo\"}]}", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetContainersStatsAsync(cancellationToken: TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!.Stats!);
    Assert.Equal("c", result.Value!.Stats![0].Id);
    Assert.Equal("demo", result.Value!.Stats![0].Name);
  }

  [Fact]
  public async Task WaitContainerAsync_Should_Post_And_Read_Bare_Status_Code() {
    HttpMethod? method = null;
    Uri? requestUri = null;
    var client = CreateClient((request, _) => {
      method = request.Method;
      requestUri = request.RequestUri;
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new StringContent("-1", Encoding.UTF8, "application/json")
      });
    });

    var result = await client.WaitContainerAsync("demo", "running", TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Equal(HttpMethod.Post, method);
    Assert.Equal("/v4.0.0/libpod/containers/demo/wait", requestUri!.AbsolutePath);
    Assert.Equal("condition=running", requestUri.Query.TrimStart('?'));
    Assert.Equal(-1, result.Value?.StatusCode);
  }

  [Fact]
  public async Task InspectPodAsync_Should_Read_Container_Objects() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("{\"Id\":\"p\",\"Containers\":[{\"Id\":\"infra\",\"Name\":\"demo-infra\",\"State\":\"created\"}]}", Encoding.UTF8, "application/json")
    }));

    var result = await client.InspectPodAsync("p", TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!.Containers!);
    Assert.Equal("infra", result.Value!.Containers![0].Id);
    Assert.Equal("demo-infra", result.Value!.Containers![0].Name);
    Assert.Equal("created", result.Value!.Containers![0].State);
  }

  [Fact]
  public async Task GetPodsStatsAsync_Should_Read_Array_Root() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("[{\"Id\":\"p\",\"Name\":\"demo\"}]", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetPodsStatsAsync(TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!);
    Assert.Equal("p", result.Value![0].Id);
    Assert.Equal("demo", result.Value![0].Name);
  }

  [Fact]
  public async Task GetContainerChangesAsync_Should_Read_Change_Objects() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("[{\"Path\":\"/etc\",\"Kind\":0}]", Encoding.UTF8, "application/json")
    }));

    var result = await client.GetContainerChangesAsync("demo", TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!);
    Assert.Equal("/etc", result.Value![0].Path);
    Assert.Equal(0, result.Value![0].Kind);
  }

  private static PodmanClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) {
    var httpClient = new HttpClient(new DelegateHttpMessageHandler(handler)) {
      BaseAddress = new Uri("http://localhost/")
    };

    return new PodmanClient(NullLogger<PodmanClient>.Instance, "http://localhost", httpClient);
  }

  private sealed class DelegateHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler
  ) : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
      handler(request, cancellationToken);
  }
}
