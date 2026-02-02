namespace MouseFood;

/// <summary>
/// Thread-safe circular buffer that stores the last 1000 log lines.
/// </summary>
public sealed class InMemoryLogBuffer
{
    private const int MaxLines = 1000;
    private readonly List<string> _lines = new();
    private readonly object _lock = new();

    public event Action<string>? LineAdded;

    public void AddLine(string line)
    {
        lock (_lock)
        {
            _lines.Add(line);
            if (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
        }
        LineAdded?.Invoke(line);
    }

    public List<string> GetLines()
    {
        lock (_lock)
        {
            return new List<string>(_lines);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _lines.Clear();
        }
    }
}

/// <summary>
/// Logger provider that writes formatted log entries to an InMemoryLogBuffer.
/// </summary>
public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly InMemoryLogBuffer _buffer;

    public InMemoryLoggerProvider(InMemoryLogBuffer buffer)
    {
        _buffer = buffer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // Shorten category name to just the class name
        var shortName = categoryName;
        var lastDot = categoryName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < categoryName.Length - 1)
            shortName = categoryName[(lastDot + 1)..];

        return new InMemoryLogger(_buffer, shortName);
    }

    public void Dispose() { }
}

/// <summary>
/// Logger that formats entries and writes them to the shared InMemoryLogBuffer.
/// </summary>
public sealed class InMemoryLogger : ILogger
{
    private readonly InMemoryLogBuffer _buffer;
    private readonly string _categoryName;

    public InMemoryLogger(InMemoryLogBuffer buffer, string categoryName)
    {
        _buffer = buffer;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var level = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };

        var message = formatter(state, exception);
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {_categoryName}: {message}";

        if (exception != null)
            line += Environment.NewLine + exception;

        _buffer.AddLine(line);
    }
}
