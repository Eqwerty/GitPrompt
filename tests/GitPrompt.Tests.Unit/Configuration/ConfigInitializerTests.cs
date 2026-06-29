using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigInitializerTests
{
    [Fact]
    public void BuildConfigContent_WhenPassedCustomConfig_ShouldUseItsValuesInsteadOfDefaults()
    {
        // Arrange
        var customConfig = new ConfigDto
        {
            Context = new ConfigDto.ContextConfig { ShowUser = false, MaxPathDepth = 3 },
            Compact = true
        };

        // Act
        var content = ConfigInitializer.BuildConfigContent(customConfig);

        // Assert
        content.Should().Contain("\"showUser\": false");
        content.Should().Contain("\"maxPathDepth\": 3");
        content.Should().Contain("\"compact\": true");
    }

    [Fact]
    public void BuildConfigContent_WhenPassedConfigWithCustomIcon_ShouldSerializeIconValue()
    {
        // Arrange
        var customConfig = new ConfigDto
        {
            Icons = new ConfigDto.IconsConfig { Ahead = "↑" }
        };

        // Act
        var content = ConfigInitializer.BuildConfigContent(customConfig);

        // Assert
        content.Should().Contain("\"ahead\": \"↑\"");
    }

    [Fact]
    public void BuildConfigContent_WhenPassedNewConfig_ShouldProduceSameOutputAsBuildDefaultConfigContent()
    {
        // Act & Assert
        ConfigInitializer.BuildConfigContent(new ConfigDto())
            .Should().Be(ConfigInitializer.BuildDefaultConfigContent());
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileIsUpToDate_ShouldNotModifyFile()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var original = ConfigInitializer.BuildDefaultConfigContent();
        File.WriteAllText(configPath, original);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(original);

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelKeyMissing_ShouldAddItWithDefaultValue()
    {
        // Arrange — config has a context group but is missing layout and commandDuration groups entirely
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "context": {
                "showUser": false
              }
            }
            """;
        File.WriteAllText(configPath, contentMissingKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"layout\":");
        result.Should().Contain("\"commandDuration\":");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelKeyMissing_ShouldPreserveExistingCustomValues()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "context": {
                "showUser": false,
                "showDomain": false,
                "showHost": false,
                "maxPathDepth": 0
              }
            }
            """;
        File.WriteAllText(configPath, contentMissingKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showUser\": false");
        result.Should().Contain("\"showHost\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenNestedKeyMissing_ShouldAddItWithDefaultValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingNestedKey = """
            {
              "cache": {
                "gitStatusTtl": 10
              }
            }
            """;
        File.WriteAllText(configPath, contentMissingNestedKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"repositoryTtl\":");
        result.Should().Contain("\"gitStatusTtl\": 10");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenKeysMissing_ShouldWriteDefaultValuesForAbsentKeys()
    {
        // Arrange — partial config; context, layout, commandDuration, showStash are all absent
        var configPath = Path.GetTempFileName();
        var contentWithOnlyCompact = """
            {
              "compact": false
            }
            """;
        File.WriteAllText(configPath, contentWithOnlyCompact);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert — absent keys are written with their actual default values
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showUser\": true");
        result.Should().Contain("\"showHost\": true");
        result.Should().Contain("\"showPath\": true");
        result.Should().Contain("\"multiline\": true");
        result.Should().Contain("\"show\": true");
        result.Should().Contain("\"showStash\": true");
        result.Should().Contain("\"startOfLine\": true");

        // Existing explicit value must be preserved
        result.Should().Contain("\"compact\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenBoolKeyExplicitlyFalse_ShouldPreserveUserFalse()
    {
        // Arrange — user explicitly set commandDuration.show to false.
        var configPath = Path.GetTempFileName();
        var contentWithExplicitFalse = """
            {
              "commandDuration": {
                "show": false,
                "minMs": null
              }
            }
            """;
        File.WriteAllText(configPath, contentWithExplicitFalse);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"show\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenCommandDurationMinMsSet_ShouldPreserveUserValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentWithThreshold = """
            {
              "commandDuration": {
                "show": true,
                "minMs": 5000
              }
            }
            """;
        File.WriteAllText(configPath, contentWithThreshold);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"minMs\": 5000");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileIsUnparseable_ShouldNotThrowAndNotModifyFile()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        const string malformed = "{ this is not valid json }}}";
        File.WriteAllText(configPath, malformed);

        // Act
        var act = () => ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        act.Should().NotThrow();
        File.ReadAllText(configPath).Should().Be(malformed);

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.jsonc");

        // Act
        var act = () => ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        act.Should().NotThrow();
    }


    [Fact]
    public void EnsureConfigFileExists_WhenFileDoesNotExist_ShouldCreateDirectoryAndWriteDefaultContent()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.jsonc");

        // Act
        ConfigInitializer.EnsureConfigFileExists(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(ConfigInitializer.BuildDefaultConfigContent());

        File.Delete(configPath);
        Directory.Delete(Path.GetDirectoryName(configPath)!);
    }

    [Fact]
    public void EnsureConfigFileExists_WhenFileAlreadyExists_ShouldNotOverwriteContent()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        const string originalContent = "custom content";
        File.WriteAllText(configPath, originalContent);

        // Act
        ConfigInitializer.EnsureConfigFileExists(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(originalContent);

        File.Delete(configPath);
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderCommandTimeoutMsAsDefault()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"commandTimeoutMs\": 0");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderCommandDurationMinMsAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — null = always show
        content.Should().Contain("\"minMs\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldNotContainUnresolvedPlaceholders()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — check that known placeholder tokens are not present
        content.Should().NotMatchRegex(@"\{[a-zA-Z]+\}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldIncludeShowCommandDuration()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("\"show\":");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderContextDefaultValues()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"showUser\": true");
        content.Should().Contain("\"showHost\": true");
        content.Should().Contain("\"showDomain\": false");
        content.Should().Contain("\"showPath\": true");
        content.Should().Contain("\"maxPathDepth\": 0");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderLayoutDefaultValues()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"multiline\": true");
        content.Should().Contain("\"newlineBefore\": false");
        content.Should().Contain("\"startOfLine\": true");
        content.Should().Contain("\"symbol\": null");  // null = automatic
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderPromptSymbolAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — null = automatic shell symbol
        content.Should().Contain("\"symbol\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllIconValuesAsDefaults()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — icons render as actual glyph values from PromptIcons/BranchLabelTokens
        content.Should().Contain($"\"ahead\": \"{PromptIcons.IconAhead}\"");
        content.Should().Contain($"\"behind\": \"{PromptIcons.IconBehind}\"");
        content.Should().Contain($"\"added\": \"{PromptIcons.IconAdded}\"");
        content.Should().Contain($"\"modified\": \"{PromptIcons.IconModified}\"");
        content.Should().Contain($"\"renamed\": \"{PromptIcons.IconRenamed}\"");
        content.Should().Contain($"\"deleted\": \"{PromptIcons.IconDeleted}\"");
        content.Should().Contain($"\"untracked\": \"{PromptIcons.IconUntracked}\"");
        content.Should().Contain($"\"conflicts\": \"{PromptIcons.IconConflicts}\"");
        content.Should().Contain($"\"stash\": \"{PromptIcons.IconStash}\"");
        content.Should().Contain($"\"noUpstreamMarker\": \"{BranchLabelTokens.NoUpstreamBranchMarker}\"");
        content.Should().Contain($"\"detachedHeadMarker\": \"{BranchLabelTokens.DetachedHeadBranchMarker}\"");
        content.Should().Contain($"\"branchOperationSeparator\": \"{BranchLabelTokens.BranchOperationSeparator}\"");
        content.Should().Contain($"\"branchLabelOpenNormal\": \"{BranchLabelTokens.NormalBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseNormal\": \"{BranchLabelTokens.NormalBranchLabelClose}\"");
        content.Should().Contain($"\"branchLabelOpenNoUpstream\": \"{BranchLabelTokens.NoUpstreamBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseNoUpstream\": \"{BranchLabelTokens.NoUpstreamBranchLabelClose}\"");
        content.Should().Contain($"\"branchLabelOpenDetached\": \"{BranchLabelTokens.DetachedBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseDetached\": \"{BranchLabelTokens.DetachedBranchLabelClose}\"");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllColorValuesAsDefaults()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — color slots render as actual ANSI color codes
        content.Should().Contain("\"user\": \"[32m\"");
        content.Should().Contain("\"host\": \"[95m\"");
        content.Should().Contain("\"path\": \"[33m\"");
        content.Should().Contain("\"branch\": \"[1;36m\"");
        content.Should().Contain("\"branchDetached\": \"[90m\"");
        content.Should().Contain("\"promptSymbol\": \"[37m\"");
    }

    [Fact]
    public void MergeWithDefaults_WhenConfigIsEmpty_ShouldApplyAllDefaults()
    {
        // Arrange
        var empty = new ConfigDto();

        // Act
        var result = ConfigInitializer.MergeWithDefaults(empty);

        // Assert
        result.Compact.Should().Be(ConfigDto.DefaultCompact);
        result.ShowStash.Should().Be(ConfigDto.DefaultShowStash);
        result.CommandTimeoutMs.Should().Be(ConfigDto.DefaultCommandTimeoutMs);
        result.Context.ShowUser.Should().Be(ConfigDto.ContextConfig.DefaultShowUser);
        result.Context.ShowDomain.Should().Be(ConfigDto.ContextConfig.DefaultShowDomain);
        result.Context.ShowHost.Should().Be(ConfigDto.ContextConfig.DefaultShowHost);
        result.Context.ShowPath.Should().Be(ConfigDto.ContextConfig.DefaultShowPath);
        result.Context.MaxPathDepth.Should().Be(ConfigDto.ContextConfig.DefaultMaxPathDepth);
        result.Layout.Multiline.Should().Be(ConfigDto.LayoutConfig.DefaultMultiline);
        result.Layout.NewlineBefore.Should().Be(ConfigDto.LayoutConfig.DefaultNewlineBefore);
        result.Layout.StartOfLine.Should().Be(ConfigDto.LayoutConfig.DefaultStartOfLine);
        result.Layout.Symbol.Should().BeNull();
        result.Layout.Prefix.Should().BeNull();
        result.CommandDuration.Show.Should().Be(ConfigDto.CommandDurationConfig.DefaultShow);
        result.CommandDuration.MinMs.Should().BeNull();
        result.Cache.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(ConfigDto.CacheConfig.DefaultGitStatusTtlSeconds));
        result.Cache.RepositoryTtl.Should().Be(TimeSpan.FromSeconds(ConfigDto.CacheConfig.DefaultRepositoryTtlSeconds));
    }

    [Fact]
    public void MergeWithDefaults_WhenUserSetsValues_ShouldOverrideDefaults()
    {
        // Arrange
        var custom = new ConfigDto
        {
            Compact = true,
            ShowStash = false,
            Context = new ConfigDto.ContextConfig { ShowUser = false, MaxPathDepth = 5 },
            Layout = new ConfigDto.LayoutConfig { Multiline = false, Symbol = "❯" },
            CommandDuration = new ConfigDto.CommandDurationConfig { Show = false, MinMs = 2000 }
        };

        // Act
        var result = ConfigInitializer.MergeWithDefaults(custom);

        // Assert
        result.Compact.Should().BeTrue();
        result.ShowStash.Should().BeFalse();
        result.Context.ShowUser.Should().BeFalse();
        result.Context.MaxPathDepth.Should().Be(5);
        result.Layout.Multiline.Should().BeFalse();
        result.Layout.Symbol.Should().Be("❯");
        result.CommandDuration.Show.Should().BeFalse();
        result.CommandDuration.MinMs.Should().Be(2000);
    }

    [Fact]
    public void MergeWithDefaults_WhenDirtyStagedIsNotSet_ShouldFallBackToDirtyIcon()
    {
        // Arrange — user sets dirty icon only; dirtyStaged is absent
        var config = new ConfigDto
        {
            Icons = new ConfigDto.IconsConfig { Dirty = "D" }
        };

        // Act
        var result = ConfigInitializer.MergeWithDefaults(config);

        // Assert
        result.Icons.Dirty.Should().Be("D");
        result.Icons.DirtyStaged.Should().Be("D");
    }

    [Fact]
    public void MergeWithDefaults_WhenDirtyStagedIsExplicitlySet_ShouldUseItsOwnValue()
    {
        // Arrange — user sets both dirty and dirtyStaged independently
        var config = new ConfigDto
        {
            Icons = new ConfigDto.IconsConfig { Dirty = "D", DirtyStaged = "S" }
        };

        // Act
        var result = ConfigInitializer.MergeWithDefaults(config);

        // Assert
        result.Icons.Dirty.Should().Be("D");
        result.Icons.DirtyStaged.Should().Be("S");
    }

    [Fact]
    public void MergeWithDefaults_WhenCustomIconsAreSet_ShouldUseUserIcons()
    {
        // Arrange
        var config = new ConfigDto
        {
            Icons = new ConfigDto.IconsConfig { Ahead = "↑", Behind = "↓", Added = "A" }
        };

        // Act
        var result = ConfigInitializer.MergeWithDefaults(config);

        // Assert
        result.Icons.Ahead.Should().Be("↑");
        result.Icons.Behind.Should().Be("↓");
        result.Icons.Added.Should().Be("A");
        result.Icons.Modified.Should().Be(PromptIcons.IconModified.ToString());
    }

    [Fact]
    public void MergeWithDefaults_WhenColorsAreAbsent_ShouldApplyDefaultAnsiCodes()
    {
        // Arrange
        var empty = new ConfigDto();

        // Act
        var result = ConfigInitializer.MergeWithDefaults(empty);

        // Assert — colors are resolved to config-file format (no leading ESC)
        result.Colors.User.Should().Be("[32m");
        result.Colors.Branch.Should().Be("[1;36m");
        result.Colors.PromptSymbol.Should().Be("[37m");
    }

    [Fact]
    public void MergeWithDefaults_WhenUserSetsColor_ShouldUseUserColor()
    {
        // Arrange
        var config = new ConfigDto
        {
            Colors = new ConfigDto.ColorsConfig { User = "#FF0000" }
        };

        // Act
        var result = ConfigInitializer.MergeWithDefaults(config);

        // Assert
        result.Colors.User.Should().Be("#FF0000");
        result.Colors.Branch.Should().Be("[1;36m"); // unchanged default
    }
}
