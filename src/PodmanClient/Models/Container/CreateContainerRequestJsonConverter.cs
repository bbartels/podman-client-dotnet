using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using MaksIT.PodmanClientDotNet.Models;

namespace MaksIT.PodmanClientDotNet.Models.Container;

[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The converter only writes known Podman request model types that are rooted by the generated JSON context.")]
[UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamicCode", Justification = "The converter only writes known Podman request model types that are rooted by the generated JSON context.")]
internal sealed class CreateContainerRequestJsonConverter : JsonConverter<CreateContainerRequest> {
  private static readonly PropertyInfo[] Properties = typeof(CreateContainerRequest)
    .GetProperties(BindingFlags.Instance | BindingFlags.Public);

  private static readonly Dictionary<string, string> PropertyNameOverrides = new(StringComparer.Ordinal) {
    [nameof(CreateContainerRequest.ContainerCreateCommand)] = "containerCreateCommand",
    [nameof(CreateContainerRequest.DependencyContainers)] = "dependencyContainers",
    [nameof(CreateContainerRequest.EnvHTTPProxy)] = "httpproxy",
    [nameof(CreateContainerRequest.EnvMerge)] = "envmerge",
    [nameof(CreateContainerRequest.HostUsers)] = "hostusers",
    [nameof(CreateContainerRequest.IDMappings)] = "idmappings",
    [nameof(CreateContainerRequest.SdnotifyMode)] = "sdnotifyMode"
  };

  public override CreateContainerRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    throw new NotSupportedException($"{nameof(CreateContainerRequest)} is only serialized by this client.");

  public override void Write(Utf8JsonWriter writer, CreateContainerRequest value, JsonSerializerOptions options) {
    writer.WriteStartObject();

    foreach (var property in Properties) {
      var propertyValue = property.GetValue(value);
      if (propertyValue is null)
        continue;

      writer.WritePropertyName(GetJsonPropertyName(property.Name));

      if (property.Name == nameof(CreateContainerRequest.Mounts)) {
        WriteMounts(writer, (List<Mount>)propertyValue, options);
        continue;
      }

      JsonSerializer.Serialize(writer, propertyValue, propertyValue.GetType(), options);
    }

    writer.WriteEndObject();
  }

  private static void WriteMounts(Utf8JsonWriter writer, List<Mount> mounts, JsonSerializerOptions options) {
    writer.WriteStartArray();

    foreach (var mount in mounts) {
      WriteMount(writer, CreateWireMount(mount), options);
    }

    writer.WriteEndArray();
  }

  private static void WriteMount(Utf8JsonWriter writer, CreateContainerMountWire mount, JsonSerializerOptions options) {
    writer.WriteStartObject();

    if (mount.Destination is not null)
      writer.WriteString("destination", mount.Destination);

    if (mount.Type is not null)
      writer.WriteString("type", mount.Type);

    if (mount.Source is not null)
      writer.WriteString("source", mount.Source);

    if (mount.Options is not null) {
      writer.WritePropertyName("options");
      writer.WriteStartArray();
      foreach (var option in mount.Options)
        writer.WriteStringValue(option);
      writer.WriteEndArray();
    }

    writer.WriteEndObject();
  }

  private static CreateContainerMountWire CreateWireMount(Mount mount) {
    ArgumentNullException.ThrowIfNull(mount);

    var type = string.IsNullOrWhiteSpace(mount.Type)
      ? null
      : mount.Type.Trim().ToLowerInvariant();

    var options = new List<string>();

    if (mount.ReadOnly)
      options.Add("ro");
    else
      options.Add("rw");

    if (mount.BindOptions is not null) {
      if (!string.IsNullOrWhiteSpace(mount.BindOptions.Propagation))
        options.Add(mount.BindOptions.Propagation);

      if (mount.BindOptions.ReadOnlyNonRecursive)
        options.Add("bind-recursive=disabled");

      if (mount.BindOptions.ReadOnlyForceRecursive)
        options.Add("bind-recursive=readonly");
    }

    if (mount.TmpfsOptions is not null) {
      if (mount.TmpfsOptions.SizeBytes > 0)
        options.Add($"size={mount.TmpfsOptions.SizeBytes}");

      if (mount.TmpfsOptions.Mode > 0)
        options.Add($"mode={Convert.ToString(mount.TmpfsOptions.Mode, 8)}");

      if (mount.TmpfsOptions.Options is not null)
        options.AddRange(mount.TmpfsOptions.Options.Where(static option => !string.IsNullOrWhiteSpace(option)));
    }

    if (mount.VolumeOptions is not null) {
      if (mount.VolumeOptions.NoCopy)
        options.Add("nocopy");

      if (!string.IsNullOrWhiteSpace(mount.VolumeOptions.Subpath))
        options.Add($"subpath={mount.VolumeOptions.Subpath}");
    }

    return new CreateContainerMountWire {
      Destination = mount.Target,
      Type = type,
      Source = type == "tmpfs" ? string.Empty : mount.Source,
      Options = options
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList()
    };
  }

  private static string GetJsonPropertyName(string propertyName) =>
    PropertyNameOverrides.TryGetValue(propertyName, out var jsonName)
      ? jsonName
      : ToSnakeCase(propertyName);

  private static string ToSnakeCase(string value) {
    if (string.IsNullOrEmpty(value))
      return value;

    var builder = new StringBuilder(value.Length + 8);

    for (var i = 0; i < value.Length; i++) {
      var current = value[i];
      var hasPrevious = i > 0;
      var hasNext = i + 1 < value.Length;

      if (char.IsUpper(current)) {
        var previous = hasPrevious ? value[i - 1] : '\0';
        var next = hasNext ? value[i + 1] : '\0';

        if (hasPrevious && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && hasNext && char.IsLower(next))))
          builder.Append('_');

        builder.Append(char.ToLowerInvariant(current));
      }
      else {
        builder.Append(current);
      }
    }

    return builder.ToString();
  }

  private sealed class CreateContainerMountWire {
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }
  }
}
