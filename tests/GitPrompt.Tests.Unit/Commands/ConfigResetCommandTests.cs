using FluentAssertions;
using GitPrompt.Commands;
using GitPrompt.Configuration;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class ConfigResetCommandTests
{
    [Fact]
    public void Run_WhenUserConfirms_ShouldOverwriteConfigWithDefaults()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        File.WriteAllText(configPath, "custom content");
        var input = new StringReader("y");

        // Act
        ConfigResetCommand.Run(configPath, input, TextWriter.Null);

        // Assert
        File.ReadAllText(configPath).Should().Be(ConfigInitializer.BuildDefaultConfigContent());

        File.Delete(configPath);
    }

    [Fact]
    public void Run_WhenUserConfirms_ShouldPrintSuccessMessage()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var input = new StringReader("y");
        var output = new StringWriter();

        // Act
        ConfigResetCommand.Run(configPath, input, output);

        // Assert
        output.ToString().Should().Contain("Config reset to defaults.");

        File.Delete(configPath);
    }

    [Fact]
    public void Run_WhenUserDeclines_ShouldNotOverwriteConfig()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        const string originalContent = "custom content";
        File.WriteAllText(configPath, originalContent);
        var input = new StringReader("n");

        // Act
        ConfigResetCommand.Run(configPath, input, TextWriter.Null);

        // Assert
        File.ReadAllText(configPath).Should().Be(originalContent);

        File.Delete(configPath);
    }

    [Fact]
    public void Run_WhenUserDeclines_ShouldPrintAbortedMessage()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var input = new StringReader("n");
        var output = new StringWriter();

        // Act
        ConfigResetCommand.Run(configPath, input, output);

        // Assert
        output.ToString().Should().Contain("Aborted.");

        File.Delete(configPath);
    }
}
