using FluentAssertions;
using Prompt.Prompting;
using Prompt.Tests.Unit.Platform;

namespace Prompt.Tests.Unit.Prompting;

public sealed class PromptSymbolBuilderTests
{
    [Fact]
    public void Build_WhenOnWindows_ShouldReturnGreaterThan()
    {
        var symbol = PromptSymbolBuilder.Build(new TestPlatformProvider(isWindows: true, user: "root"));

        symbol.Should().Be(">");
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        var symbol = PromptSymbolBuilder.Build(new TestPlatformProvider(isWindows: false, user: "root"));

        symbol.Should().Be("#");
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        var symbol = PromptSymbolBuilder.Build(new TestPlatformProvider(isWindows: false, user: "me"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void Build_WhenOnUnixAndWindowsUsernameIsRoot_ShouldReturnDollar()
    {
        var symbol = PromptSymbolBuilder.Build(new TestPlatformProvider(isWindows: false, windowsUserName: "root"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void Build_WhenOnUnixAndNoUserSet_ShouldReturnDollar()
    {
        var symbol = PromptSymbolBuilder.Build(new TestPlatformProvider(isWindows: false));

        symbol.Should().Be("$");
    }
}
