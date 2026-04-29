using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitPrompt.Configuration;

internal sealed record Config
{
    [JsonInclude]
    internal CacheConfig Cache { get; init; } = new();

    [JsonInclude]
    [JsonPropertyName("showUser")]
    internal bool ShowUser { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("showHost")]
    internal bool ShowHost { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("maxPathDepth")]
    internal int MaxPathDepth { get; init; } = 0;

    [JsonInclude]
    [JsonPropertyName("multilinePrompt")]
    internal bool MultilinePrompt { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("newlineBeforePrompt")]
    internal bool NewlineBeforePrompt { get; init; } = false;

    [JsonInclude]
    [JsonPropertyName("showCommandDuration")]
    internal bool ShowCommandDuration { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("promptSymbol")]
    internal string? PromptSymbol { get; init; }

    [JsonInclude]
    internal IconsConfig Icons { get; init; } = new();

    [JsonInclude]
    [JsonPropertyName("commandTimeoutMs")]
    internal double? CommandTimeoutMs { get; init; }

    [JsonIgnore]
    internal TimeSpan? CommandTimeout
    {
        get
        {
            var ms = CommandTimeoutMs ?? 2000.0;

            return ms > 0 && double.IsFinite(ms) ? TimeSpan.FromMilliseconds(ms) : null;
        }
    }

    internal sealed record CacheConfig
    {
        [JsonInclude]
        [JsonPropertyName("gitStatusTtl")]
        internal double? GitStatusTtlSeconds { get; init; }

        [JsonInclude]
        [JsonPropertyName("repositoryTtl")]
        internal double? RepositoryTtlSeconds { get; init; }

        [JsonIgnore]
        internal TimeSpan GitStatusTtl => TimeSpan.FromSeconds(GitStatusTtlSeconds ?? 5.0);

        [JsonIgnore]
        internal TimeSpan RepositoryTtl => TimeSpan.FromSeconds(RepositoryTtlSeconds ?? 60.0);
    }

    internal sealed record IconsConfig
    {
        [JsonInclude]
        [JsonPropertyName("ahead")]
        internal string? Ahead { get; init; }

        [JsonInclude]
        [JsonPropertyName("behind")]
        internal string? Behind { get; init; }

        [JsonInclude]
        [JsonPropertyName("added")]
        internal string? Added { get; init; }

        [JsonInclude]
        [JsonPropertyName("modified")]
        internal string? Modified { get; init; }

        [JsonInclude]
        [JsonPropertyName("renamed")]
        internal string? Renamed { get; init; }

        [JsonInclude]
        [JsonPropertyName("deleted")]
        internal string? Deleted { get; init; }

        [JsonInclude]
        [JsonPropertyName("untracked")]
        internal string? Untracked { get; init; }

        [JsonInclude]
        [JsonPropertyName("conflicts")]
        internal string? Conflicts { get; init; }

        [JsonInclude]
        [JsonPropertyName("stash")]
        internal string? Stash { get; init; }
    }
}

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal partial class ConfigJsonContext : JsonSerializerContext;
