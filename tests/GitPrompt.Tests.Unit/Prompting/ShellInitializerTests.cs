using FluentAssertions;
using GitPrompt.Prompting;

namespace GitPrompt.Tests.Unit.Prompting;

public sealed class ShellInitializerTests
{
    [Fact]
    public void GenerateBashInit_ShouldContainPromptCommand()
    {
        var script = ShellInitializer.GenerateBashInit("\\w \\$ ");

        script.Should().Contain("PROMPT_COMMAND");
    }

    [Fact]
    public void GenerateBashInit_ShouldContainDebugTrap()
    {
        var script = ShellInitializer.GenerateBashInit("\\w \\$ ");

        script.Should().Contain("trap '__gitprompt_debug_trap' DEBUG");
    }

    [Fact]
    public void GenerateBashInit_ShouldContainInvalidateCacheCall()
    {
        var script = ShellInitializer.GenerateBashInit("\\w \\$ ");

        script.Should().Contain("--invalidate-status-cache");
    }

    [Fact]
    public void GenerateBashInit_ShouldResolveBinaryFromPath()
    {
        var script = ShellInitializer.GenerateBashInit("\\w \\$ ");

        script.Should().Contain("command -v gitprompt");
    }

    [Fact]
    public void GenerateBashInit_ShouldEmbedFallbackPs1()
    {
        const string fallback = "\\w >";
        var script = ShellInitializer.GenerateBashInit(fallback);

        script.Should().Contain($"PS1='{fallback}'");
    }

    [Fact]
    public void GenerateBashInit_ShouldNotContainHardcodedAbsolutePath()
    {
        var script = ShellInitializer.GenerateBashInit("\\w \\$ ");

        // The script must resolve the binary from PATH at shell startup, not bake in an absolute path.
        script.Should().NotContain("/home/");
        script.Should().NotContain("/Users/");
        script.Should().NotContain("C:\\");
    }
}
