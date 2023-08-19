namespace fluxel;

public static class Logger
{
    public static void Log(string message, LogLevel level = LogLevel.Verbose)
    {
        var severity = getSeverity(level).PadRight(8)[..8];

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ForegroundColor = getColor(level);
        Console.Write($"{severity} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{message}");
    }

    private static ConsoleColor getColor(LogLevel logLevel) => logLevel switch {
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Verbose => ConsoleColor.Cyan,
        LogLevel.Debug => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };

    private static string getSeverity(LogLevel logLevel) => logLevel switch {
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Verbose => "Verbose",
        LogLevel.Debug => "Debug",
        _ => "???"
    };
}

public enum LogLevel
{
    Debug,
    Verbose,
    Warning,
    Error
}
