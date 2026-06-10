using System.Globalization;
using System.Text.Json;
using GitPrompt.Constants;
using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            var configPath = AppPaths.GetConfigFilePath();
            EnsureConfigFileExists(configPath);
            MigrateConfigIfNeeded(configPath);
        }
        catch
        {
            // Non-critical: the binary works fine with default settings even when the config file is absent.
        }
    }

    internal static void EnsureConfigFileExists(string configPath)
    {
        if (File.Exists(configPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        File.WriteAllText(configPath, BuildDefaultConfigContent());
    }

    internal static string BuildDefaultConfigContent() => BuildConfigContent(new ConfigDto());

    internal static string BuildConfigContent(ConfigDto configDto)
    {
        var config = MergeWithDefaults(configDto);

        using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.jsonc")!;
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();

        return template
            .Replace("{gitStatusTtl}", JsonDouble(config.Cache.GitStatusTtl.TotalSeconds))
            .Replace("{repositoryTtl}", JsonDouble(config.Cache.RepositoryTtl.TotalSeconds))
            .Replace("{commandTimeoutMs}", JsonDouble(config.CommandTimeoutMs))
            .Replace("{commandDurationShow}", JsonBool(config.CommandDuration.Show))
            .Replace("{commandDurationMinMs}", JsonNullableDouble(config.CommandDuration.MinMs))
            .Replace("{showUser}", JsonBool(config.Context.ShowUser))
            .Replace("{showDomain}", JsonBool(config.Context.ShowDomain))
            .Replace("{showHost}", JsonBool(config.Context.ShowHost))
            .Replace("{showPath}", JsonBool(config.Context.ShowPath))
            .Replace("{maxPathDepth}", JsonInt(config.Context.MaxPathDepth))
            .Replace("{multiline}", JsonBool(config.Layout.Multiline))
            .Replace("{newlineBefore}", JsonBool(config.Layout.NewlineBefore))
            .Replace("{startOfLine}", JsonBool(config.Layout.StartOfLine))
            .Replace("{symbol}", JsonValue(config.Layout.Symbol))
            .Replace("{prefix}", JsonValue(config.Layout.Prefix))
            .Replace("{compact}", JsonBool(config.Compact))
            .Replace("{showStash}", JsonBool(config.ShowStash))
            .Replace("{iconAhead}", JsonValue(config.Icons.Ahead))
            .Replace("{iconBehind}", JsonValue(config.Icons.Behind))
            .Replace("{iconAdded}", JsonValue(config.Icons.Added))
            .Replace("{iconModified}", JsonValue(config.Icons.Modified))
            .Replace("{iconRenamed}", JsonValue(config.Icons.Renamed))
            .Replace("{iconDeleted}", JsonValue(config.Icons.Deleted))
            .Replace("{iconUntracked}", JsonValue(config.Icons.Untracked))
            .Replace("{iconConflicts}", JsonValue(config.Icons.Conflicts))
            .Replace("{iconStash}", JsonValue(config.Icons.Stash))
            .Replace("{iconDirty}", JsonValue(config.Icons.Dirty))
            .Replace("{iconClean}", JsonValue(config.Icons.Clean))
            .Replace("{iconNoUpstreamMarker}", JsonValue(config.Icons.NoUpstreamMarker))
            .Replace("{iconGoneUpstreamMarker}", JsonValue(config.Icons.GoneUpstreamMarker))
            .Replace("{iconDetachedHeadMarker}", JsonValue(config.Icons.DetachedHeadMarker))
            .Replace("{iconBranchOperationSeparator}", JsonValue(config.Icons.BranchOperationSeparator))
            .Replace("{iconBranchLabelOpenNormal}", JsonValue(config.Icons.BranchLabelOpenNormal))
            .Replace("{iconBranchLabelCloseNormal}", JsonValue(config.Icons.BranchLabelCloseNormal))
            .Replace("{iconBranchLabelOpenNoUpstream}", JsonValue(config.Icons.BranchLabelOpenNoUpstream))
            .Replace("{iconBranchLabelCloseNoUpstream}", JsonValue(config.Icons.BranchLabelCloseNoUpstream))
            .Replace("{iconBranchLabelOpenGoneUpstream}", JsonValue(config.Icons.BranchLabelOpenGoneUpstream))
            .Replace("{iconBranchLabelCloseGoneUpstream}", JsonValue(config.Icons.BranchLabelCloseGoneUpstream))
            .Replace("{iconBranchLabelOpenDetached}", JsonValue(config.Icons.BranchLabelOpenDetached))
            .Replace("{iconBranchLabelCloseDetached}", JsonValue(config.Icons.BranchLabelCloseDetached))
            .Replace("{colorUser}", JsonValue(config.Colors.User))
            .Replace("{colorHost}", JsonValue(config.Colors.Host))
            .Replace("{colorPath}", JsonValue(config.Colors.Path))
            .Replace("{colorCommandDuration}", JsonValue(config.Colors.CommandDuration))
            .Replace("{colorBranch}", JsonValue(config.Colors.Branch))
            .Replace("{colorBranchNoUpstream}", JsonValue(config.Colors.BranchNoUpstream))
            .Replace("{colorBranchGoneUpstream}", JsonValue(config.Colors.BranchGoneUpstream))
            .Replace("{colorBranchDetached}", JsonValue(config.Colors.BranchDetached))
            .Replace("{colorAhead}", JsonValue(config.Colors.Ahead))
            .Replace("{colorBehind}", JsonValue(config.Colors.Behind))
            .Replace("{colorStaged}", JsonValue(config.Colors.Staged))
            .Replace("{colorUnstaged}", JsonValue(config.Colors.Unstaged))
            .Replace("{colorUntracked}", JsonValue(config.Colors.Untracked))
            .Replace("{colorStash}", JsonValue(config.Colors.Stash))
            .Replace("{colorConflict}", JsonValue(config.Colors.Conflict))
            .Replace("{colorDirty}", JsonValue(config.Colors.Dirty))
            .Replace("{colorClean}", JsonValue(config.Colors.Clean))
            .Replace("{colorMissingPath}", JsonValue(config.Colors.MissingPath))
            .Replace("{colorTimeout}", JsonValue(config.Colors.Timeout))
            .Replace("{colorPromptSymbol}", JsonValue(config.Colors.PromptSymbol))
            .Replace("{colorPrefix}", JsonValue(config.Colors.Prefix));
    }

    private static string JsonBool(bool value) => value ? "true" : "false";

    private static string JsonInt(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string JsonDouble(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string JsonNullableDouble(double? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";

    private static string JsonValue(string? value) => value is null ? "null" : $"\"{value}\"";

    private static string ColorDisplayValue(string ansiColor)
    {
        if (ansiColor.Length > 0 && ansiColor[0] is '\e')
        {
            return ansiColor[1..];
        }

        return ansiColor;
    }

    internal static void MigrateConfigIfNeeded(string configPath)
    {
        string fileContent;

        try
        {
            fileContent = File.ReadAllText(configPath);
        }
        catch
        {
            return;
        }

        var jsonOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        JsonDocument userDoc;

        try
        {
            userDoc = JsonDocument.Parse(fileContent, jsonOptions);
        }
        catch
        {
            return;
        }

        using (userDoc)
        {
            var defaultContent = BuildDefaultConfigContent();
            using var defaultDoc = JsonDocument.Parse(defaultContent, jsonOptions);

            if (!HasMissingKeys(defaultDoc.RootElement, userDoc.RootElement))
            {
                return;
            }

            ConfigDto userConfig;

            try
            {
                userConfig = JsonSerializer.Deserialize(fileContent, ConfigJsonContext.Default.ConfigDto) ?? new ConfigDto();
            }
            catch
            {
                return;
            }

            try
            {
                File.WriteAllText(configPath, BuildConfigContent(userConfig));
            }
            catch
            {
                // Non-critical: if writing fails, the old file is preserved as-is.
            }
        }
    }

    internal static Config MergeWithDefaults(ConfigDto userConfig)
    {
        return new Config
        {
            Compact = userConfig.Compact ?? ConfigDto.DefaultCompact,
            ShowStash = userConfig.ShowStash ?? ConfigDto.DefaultShowStash,
            CommandTimeoutMs = userConfig.CommandTimeoutMs ?? ConfigDto.DefaultCommandTimeoutMs,
            Cache = new Config.CacheConfig
            {
                GitStatusTtl = TimeSpan.FromSeconds(userConfig.Cache?.GitStatusTtlSeconds ?? ConfigDto.CacheConfig.DefaultGitStatusTtlSeconds),
                RepositoryTtl = TimeSpan.FromSeconds(userConfig.Cache?.RepositoryTtlSeconds ?? ConfigDto.CacheConfig.DefaultRepositoryTtlSeconds)
            },
            CommandDuration = new Config.CommandDurationConfig
            {
                Show = userConfig.CommandDuration?.Show ?? ConfigDto.CommandDurationConfig.DefaultShow,
                MinMs = userConfig.CommandDuration?.MinMs
            },
            Context = new Config.ContextConfig
            {
                ShowUser = userConfig.Context?.ShowUser ?? ConfigDto.ContextConfig.DefaultShowUser,
                ShowDomain = userConfig.Context?.ShowDomain ?? ConfigDto.ContextConfig.DefaultShowDomain,
                ShowHost = userConfig.Context?.ShowHost ?? ConfigDto.ContextConfig.DefaultShowHost,
                ShowPath = userConfig.Context?.ShowPath ?? ConfigDto.ContextConfig.DefaultShowPath,
                MaxPathDepth = userConfig.Context?.MaxPathDepth ?? ConfigDto.ContextConfig.DefaultMaxPathDepth
            },
            Layout = new Config.LayoutConfig
            {
                Multiline = userConfig.Layout?.Multiline ?? ConfigDto.LayoutConfig.DefaultMultiline,
                NewlineBefore = userConfig.Layout?.NewlineBefore ?? ConfigDto.LayoutConfig.DefaultNewlineBefore,
                StartOfLine = userConfig.Layout?.StartOfLine ?? ConfigDto.LayoutConfig.DefaultStartOfLine,
                Symbol = userConfig.Layout?.Symbol,
                Prefix = userConfig.Layout?.Prefix
            },
            Icons = new Config.IconsConfig
            {
                Ahead = userConfig.Icons?.Ahead ?? PromptIcons.IconAhead.ToString(),
                Behind = userConfig.Icons?.Behind ?? PromptIcons.IconBehind.ToString(),
                Added = userConfig.Icons?.Added ?? PromptIcons.IconAdded.ToString(),
                Modified = userConfig.Icons?.Modified ?? PromptIcons.IconModified.ToString(),
                Renamed = userConfig.Icons?.Renamed ?? PromptIcons.IconRenamed.ToString(),
                Deleted = userConfig.Icons?.Deleted ?? PromptIcons.IconDeleted.ToString(),
                Untracked = userConfig.Icons?.Untracked ?? PromptIcons.IconUntracked.ToString(),
                Conflicts = userConfig.Icons?.Conflicts ?? PromptIcons.IconConflicts.ToString(),
                Stash = userConfig.Icons?.Stash ?? PromptIcons.IconStash.ToString(),
                Dirty = userConfig.Icons?.Dirty ?? PromptIcons.IconDirty.ToString(),
                DirtyStaged = userConfig.Icons?.DirtyStaged ?? userConfig.Icons?.Dirty ?? PromptIcons.IconDirty.ToString(),
                Clean = userConfig.Icons?.Clean ?? PromptIcons.IconClean.ToString(),
                NoUpstreamMarker = userConfig.Icons?.NoUpstreamMarker ?? BranchLabelTokens.NoUpstreamBranchMarker,
                GoneUpstreamMarker = userConfig.Icons?.GoneUpstreamMarker ?? BranchLabelTokens.GoneUpstreamBranchMarker,
                DetachedHeadMarker = userConfig.Icons?.DetachedHeadMarker ?? BranchLabelTokens.DetachedHeadBranchMarker,
                BranchLabelOpenNormal = userConfig.Icons?.BranchLabelOpenNormal ?? BranchLabelTokens.NormalBranchLabelOpen,
                BranchLabelCloseNormal = userConfig.Icons?.BranchLabelCloseNormal ?? BranchLabelTokens.NormalBranchLabelClose,
                BranchLabelOpenNoUpstream = userConfig.Icons?.BranchLabelOpenNoUpstream ?? BranchLabelTokens.NoUpstreamBranchLabelOpen,
                BranchLabelCloseNoUpstream = userConfig.Icons?.BranchLabelCloseNoUpstream ?? BranchLabelTokens.NoUpstreamBranchLabelClose,
                BranchLabelOpenGoneUpstream = userConfig.Icons?.BranchLabelOpenGoneUpstream ?? BranchLabelTokens.GoneUpstreamBranchLabelOpen,
                BranchLabelCloseGoneUpstream = userConfig.Icons?.BranchLabelCloseGoneUpstream ?? BranchLabelTokens.GoneUpstreamBranchLabelClose,
                BranchLabelOpenDetached = userConfig.Icons?.BranchLabelOpenDetached ?? BranchLabelTokens.DetachedBranchLabelOpen,
                BranchLabelCloseDetached = userConfig.Icons?.BranchLabelCloseDetached ?? BranchLabelTokens.DetachedBranchLabelClose,
                BranchOperationSeparator = userConfig.Icons?.BranchOperationSeparator ?? BranchLabelTokens.BranchOperationSeparator
            },
            Colors = new Config.ColorsConfig
            {
                User = userConfig.Colors?.User ?? ColorDisplayValue(AnsiColors.Green),
                Host = userConfig.Colors?.Host ?? ColorDisplayValue(AnsiColors.Magenta),
                Path = userConfig.Colors?.Path ?? ColorDisplayValue(AnsiColors.Orange),
                CommandDuration = userConfig.Colors?.CommandDuration ?? ColorDisplayValue(AnsiColors.Magenta),
                Branch = userConfig.Colors?.Branch ?? ColorDisplayValue(AnsiColors.BoldCyan),
                BranchNoUpstream = userConfig.Colors?.BranchNoUpstream ?? ColorDisplayValue(AnsiColors.BoldCyan),
                BranchGoneUpstream = userConfig.Colors?.BranchGoneUpstream ?? ColorDisplayValue(AnsiColors.BoldCyan),
                BranchDetached = userConfig.Colors?.BranchDetached ?? ColorDisplayValue(AnsiColors.NormalYellow),
                Ahead = userConfig.Colors?.Ahead ?? ColorDisplayValue(AnsiColors.BoldCyan),
                Behind = userConfig.Colors?.Behind ?? ColorDisplayValue(AnsiColors.BoldCyan),
                Staged = userConfig.Colors?.Staged ?? ColorDisplayValue(AnsiColors.Green),
                Unstaged = userConfig.Colors?.Unstaged ?? ColorDisplayValue(AnsiColors.Red),
                Untracked = userConfig.Colors?.Untracked ?? ColorDisplayValue(AnsiColors.Red),
                Stash = userConfig.Colors?.Stash ?? ColorDisplayValue(AnsiColors.Magenta),
                Conflict = userConfig.Colors?.Conflict ?? ColorDisplayValue(AnsiColors.Red),
                Dirty = userConfig.Colors?.Dirty ?? ColorDisplayValue(AnsiColors.Orange),
                DirtyStaged = userConfig.Colors?.DirtyStaged ?? ColorDisplayValue(AnsiColors.Green),
                Clean = userConfig.Colors?.Clean ?? ColorDisplayValue(AnsiColors.Green),
                MissingPath = userConfig.Colors?.MissingPath ?? ColorDisplayValue(AnsiColors.Red),
                Timeout = userConfig.Colors?.Timeout ?? ColorDisplayValue(AnsiColors.Yellow),
                PromptSymbol = userConfig.Colors?.PromptSymbol ?? ColorDisplayValue(AnsiColors.White),
                Prefix = userConfig.Colors?.Prefix ?? ColorDisplayValue(AnsiColors.White)
            }
        };
    }

    private static bool HasMissingKeys(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind is not JsonValueKind.Object || actual.ValueKind is not JsonValueKind.Object)
        {
            return false;
        }

        foreach (var expectedProp in expected.EnumerateObject())
        {
            if (!actual.TryGetProperty(expectedProp.Name, out var actualValue))
            {
                return true;
            }

            if (HasMissingKeys(expectedProp.Value, actualValue))
            {
                return true;
            }
        }

        return false;
    }
}
