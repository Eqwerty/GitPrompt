using FluentAssertions;
using GitPrompt.Platform;

namespace GitPrompt.Tests.Unit.Platform;

public sealed class PlatformProviderTests
{
    [Fact]
    public void ResolveHost_WhenMsystemIsSet_ShouldReturnMsystem()
    {
        // Act
        var host = PlatformProvider.ResolveHost("MINGW64", "workstation");

        // Assert
        host.Should().Be("MINGW64");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ResolveHost_WhenMsystemIsAbsent_ShouldReturnMachineName(string? msystem)
    {
        // Act
        var host = PlatformProvider.ResolveHost(msystem, "workstation");

        // Assert
        host.Should().Be("workstation");
    }

}
