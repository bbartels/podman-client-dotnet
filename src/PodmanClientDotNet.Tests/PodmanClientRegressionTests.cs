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
  public async Task ListContainersAsync_Should_Read_Rfc3339_Created() {
    var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StringContent("[{\"Id\":\"x\",\"Names\":[\"x\"],\"Created\":\"2026-07-08T11:18:26.416458796+01:00\"}]", Encoding.UTF8, "application/json")
    }));

    var result = await client.ListContainersAsync(cancellationToken: TestContext.Current.CancellationToken);

    Assert.True(result.IsSuccess, string.Join("; ", result.Messages));
    Assert.Single(result.Value!);
    Assert.Equal("2026-07-08T11:18:26.4164587+01:00", result.Value![0].Created.ToString("O"));
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
