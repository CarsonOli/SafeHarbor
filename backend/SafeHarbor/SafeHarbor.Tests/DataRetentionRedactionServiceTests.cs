using SafeHarbor.Services;

namespace SafeHarbor.Tests;

public class DataRetentionRedactionServiceTests
{
    private readonly DataRetentionRedactionService _service = new();

    [Fact]
    public void RedactFreeText_ReturnsRedactedMarker_ForNonEmptyInput()
    {
        var result = _service.RedactFreeText("Sensitive case note");

        Assert.Equal("[REDACTED]", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactFreeText_ReturnsEmpty_ForBlankInput(string input)
    {
        var result = _service.RedactFreeText(input);

        Assert.Equal(string.Empty, result);
    }
}
