using Microsoft.Extensions.Logging;

namespace MouseFood.Tests;

public class InMemoryLogBufferTests
{
    [Fact]
    public void AddLine_StoresLine()
    {
        var buffer = new InMemoryLogBuffer();

        buffer.AddLine("test line");

        var lines = buffer.GetLines();
        Assert.Single(lines);
        Assert.Equal("test line", lines[0]);
    }

    [Fact]
    public void AddLine_EnforcesMaxLimit()
    {
        var buffer = new InMemoryLogBuffer();

        for (int i = 0; i < 1050; i++)
            buffer.AddLine($"line {i}");

        var lines = buffer.GetLines();
        Assert.Equal(1000, lines.Count);
        Assert.Equal("line 50", lines[0]);
        Assert.Equal("line 1049", lines[999]);
    }

    [Fact]
    public void AddLine_FiresLineAddedEvent()
    {
        var buffer = new InMemoryLogBuffer();
        string? received = null;
        buffer.LineAdded += line => received = line;

        buffer.AddLine("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void GetLines_ReturnsSnapshot()
    {
        var buffer = new InMemoryLogBuffer();
        buffer.AddLine("line 1");

        var snapshot = buffer.GetLines();
        buffer.AddLine("line 2");

        Assert.Single(snapshot);
    }

    [Fact]
    public void Clear_RemovesAllLines()
    {
        var buffer = new InMemoryLogBuffer();
        buffer.AddLine("line 1");
        buffer.AddLine("line 2");

        buffer.Clear();

        Assert.Empty(buffer.GetLines());
    }
}

public class InMemoryLoggerTests
{
    [Fact]
    public void Log_WritesFormattedLineToBuffer()
    {
        var buffer = new InMemoryLogBuffer();
        var logger = new InMemoryLogger(buffer, "TestCategory");

        logger.LogInformation("Hello {Name}", "World");

        var lines = buffer.GetLines();
        Assert.Single(lines);
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\] \[INF\] TestCategory: Hello World", lines[0]);
    }

    [Fact]
    public void Log_IncludesExceptionDetails()
    {
        var buffer = new InMemoryLogBuffer();
        var logger = new InMemoryLogger(buffer, "Test");
        var ex = new InvalidOperationException("test error");

        logger.Log(LogLevel.Error, 0, "Something failed", ex, (s, e) => s);

        var lines = buffer.GetLines();
        Assert.Single(lines);
        Assert.Contains("[ERR]", lines[0]);
        Assert.Contains("test error", lines[0]);
    }

    [Fact]
    public void Log_NoneLevel_IsIgnored()
    {
        var buffer = new InMemoryLogBuffer();
        var logger = new InMemoryLogger(buffer, "Test");

        logger.Log(LogLevel.None, 0, "ignored", null, (s, e) => s);

        Assert.Empty(buffer.GetLines());
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRC")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Critical, "CRT")]
    public void Log_FormatsLevelCorrectly(LogLevel level, string expected)
    {
        var buffer = new InMemoryLogBuffer();
        var logger = new InMemoryLogger(buffer, "Test");

        logger.Log(level, 0, "msg", null, (s, e) => s);

        Assert.Contains($"[{expected}]", buffer.GetLines()[0]);
    }

    [Fact]
    public void IsEnabled_ReturnsFalseForNone()
    {
        var logger = new InMemoryLogger(new InMemoryLogBuffer(), "Test");

        Assert.False(logger.IsEnabled(LogLevel.None));
        Assert.True(logger.IsEnabled(LogLevel.Debug));
    }
}

public class InMemoryLoggerProviderTests
{
    [Fact]
    public void CreateLogger_ShortensCategory()
    {
        var buffer = new InMemoryLogBuffer();
        var provider = new InMemoryLoggerProvider(buffer);

        var logger = provider.CreateLogger("MouseFood.SynapseLogParser");
        logger.LogInformation("test");

        Assert.Contains("SynapseLogParser:", buffer.GetLines()[0]);
    }

    [Fact]
    public void CreateLogger_HandlesNoDot()
    {
        var buffer = new InMemoryLogBuffer();
        var provider = new InMemoryLoggerProvider(buffer);

        var logger = provider.CreateLogger("Worker");
        logger.LogInformation("test");

        Assert.Contains("Worker:", buffer.GetLines()[0]);
    }
}
