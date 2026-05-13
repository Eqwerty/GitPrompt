using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class AliasesCommandTests
{
    [Fact]
    public void Run_WhenAliasesFileDoesNotExist_ShouldWriteInformativeErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.Run(nonExistentPath, errorOutput);

        // Assert
        var error = errorOutput.ToString();
        error.Should().Contain("git aliases not found at:");
        error.Should().Contain("gitprompt update aliases");
        error.Should().Contain(nonExistentPath);
    }

    [Fact]
    public void RunEnable_WhenAliasesFileDoesNotExist_ShouldWriteInformativeErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.RunEnable(nonExistentPath, TextWriter.Null, errorOutput);

        // Assert
        var error = errorOutput.ToString();
        error.Should().Contain("git aliases not found at:");
        error.Should().Contain("gitprompt update aliases");
        error.Should().Contain(nonExistentPath);
    }

    [Fact]
    public void RunEnable_WhenAliasesFileExists_ShouldOutputSourceCommand()
    {
        // Arrange
        var aliasesPath = Path.GetTempFileName();
        File.WriteAllText(aliasesPath, "alias gs=\"git status\"");
        var scriptOutput = new StringWriter();

        try
        {
            // Act
            AliasesCommand.RunEnable(aliasesPath, scriptOutput, TextWriter.Null);

            // Assert
            scriptOutput.ToString().Should().Contain($". '{aliasesPath}'");
        }
        finally
        {
            File.Delete(aliasesPath);
        }
    }

    [Fact]
    public void RunEnable_WhenAliasesFileExists_ShouldSetEnabledVariable()
    {
        // Arrange
        var aliasesPath = Path.GetTempFileName();
        File.WriteAllText(aliasesPath, "alias gs=\"git status\"");
        var scriptOutput = new StringWriter();

        try
        {
            // Act
            AliasesCommand.RunEnable(aliasesPath, scriptOutput, TextWriter.Null);

            // Assert
            scriptOutput.ToString().Should().Contain("_GITPROMPT_ALIASES_ENABLED=1");
        }
        finally
        {
            File.Delete(aliasesPath);
        }
    }

    [Fact]
    public void RunDisable_WhenAliasesFileDoesNotExist_ShouldWriteInformativeErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.RunDisable(nonExistentPath, TextWriter.Null, errorOutput);

        // Assert
        var error = errorOutput.ToString();
        error.Should().Contain("git aliases not found at:");
        error.Should().Contain("gitprompt update aliases");
        error.Should().Contain(nonExistentPath);
    }

    [Fact]
    public void RunDisable_WhenAliasesFileExists_ShouldOutputUnaliasCommand()
    {
        // Arrange
        var aliasesPath = Path.GetTempFileName();
        File.WriteAllText(aliasesPath, "alias gs=\"git status\"\nalias gd=\"git diff\"");
        var scriptOutput = new StringWriter();

        try
        {
            // Act
            AliasesCommand.RunDisable(aliasesPath, scriptOutput, TextWriter.Null);

            // Assert
            var script = scriptOutput.ToString();
            script.Should().Contain("unalias");
            script.Should().Contain("gs");
            script.Should().Contain("gd");
        }
        finally
        {
            File.Delete(aliasesPath);
        }
    }

    [Fact]
    public void RunDisable_WhenAliasesFileExists_ShouldOutputUnsetFunctionCommand()
    {
        // Arrange
        var aliasesPath = Path.GetTempFileName();
        File.WriteAllText(aliasesPath, "function gam() {\n  echo hello\n}\nfunction gl() {\n  echo world\n}");
        var scriptOutput = new StringWriter();

        try
        {
            // Act
            AliasesCommand.RunDisable(aliasesPath, scriptOutput, TextWriter.Null);

            // Assert
            var script = scriptOutput.ToString();
            script.Should().Contain("unset -f");
            script.Should().Contain("gam");
            script.Should().Contain("gl");
        }
        finally
        {
            File.Delete(aliasesPath);
        }
    }

    [Fact]
    public void RunDisable_WhenAliasesFileExists_ShouldSetDisabledVariable()
    {
        // Arrange
        var aliasesPath = Path.GetTempFileName();
        File.WriteAllText(aliasesPath, "alias gs=\"git status\"");
        var scriptOutput = new StringWriter();

        try
        {
            // Act
            AliasesCommand.RunDisable(aliasesPath, scriptOutput, TextWriter.Null);

            // Assert
            scriptOutput.ToString().Should().Contain("_GITPROMPT_ALIASES_ENABLED=0");
        }
        finally
        {
            File.Delete(aliasesPath);
        }
    }
}

