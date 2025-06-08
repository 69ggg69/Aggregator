using Microsoft.Extensions.Logging;

namespace Aggregator.Tests.Helpers;

/// <summary>
/// Простой хелпер для создания реальных логгеров в тестах
/// Выводит логи прямо в консоль
/// </summary>
public static class TestLogger
{
    /// <summary>
    /// Создает реальный консольный логгер для тестов
    /// </summary>
    public static ILogger<T> Create<T>()
    {
        using var factory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.TimestampFormat = "[HH:mm:ss.fff] ";
                });
        });

        return factory.CreateLogger<T>();
    }

    /// <summary>
    /// Создает реальный консольный логгер с указанным именем
    /// </summary>
    public static ILogger CreateNamed(string name)
    {
        using var factory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.TimestampFormat = "[HH:mm:ss.fff] ";
                });
        });

        return factory.CreateLogger(name);
    }
} 