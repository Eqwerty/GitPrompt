using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitPrompt.Configuration;

internal sealed record Config
{
    internal CacheConfig Cache { get; init; } = new();

    internal sealed record CacheConfig
    {
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        internal TimeSpan GitStatusTtl { get; init; } = TimeSpan.FromSeconds(5);

        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        internal TimeSpan RepositoryTtl { get; init; } = TimeSpan.FromSeconds(60);
    }
}

internal sealed class TimeSpanSecondsConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TimeSpan.FromSeconds(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.TotalSeconds);
    }
}

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal partial class ConfigJsonContext : JsonSerializerContext;
