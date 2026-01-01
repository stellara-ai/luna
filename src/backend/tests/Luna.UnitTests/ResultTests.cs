namespace Luna.UnitTests;

using Xunit;
using Luna.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Result_Success_MatchesCorrectly()
    {
        // Arrange
        Result<int> result = new Result<int>.Success(42);

        // Act
        var value = result.Match(
            onSuccess: v => v * 2,
            onFailure: (msg, _) => -1
        );

        // Assert
        Assert.Equal(84, value);
    }

    [Fact]
    public void Result_Failure_MatchesCorrectly()
    {
        // Arrange
        Result<int> result = new Result<int>.Failure("test error");

        // Act
        var value = result.Match(
            onSuccess: v => -1,
            onFailure: (msg, _) => msg.Length
        );

        // Assert
        Assert.Equal(10, value);
    }
}
