namespace Aggregator.Tests.Helpers;

/// <summary>
/// САМЫЙ ПРОСТОЙ способ логирования
/// Просто статические методы для вывода в консоль
/// </summary>
public static class Log
{
    /// <summary>
    /// Выводит информационное сообщение
    /// </summary>
    public static void Info(string message)
    {
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    /// <summary>
    /// Выводит информационное сообщение с параметрами
    /// </summary>
    public static void Info(string message, params object[] args)
    {
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} {string.Format(message, args)}");
    }

    /// <summary>
    /// Выводит отладочное сообщение
    /// </summary>
    public static void Debug(string message)
    {
        Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    /// <summary>
    /// Выводит предупреждение
    /// </summary>
    public static void Warning(string message)
    {
        Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    /// <summary>
    /// Выводит ошибку
    /// </summary>
    public static void Error(string message)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    /// <summary>
    /// Выводит ошибку с исключением
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
        Console.WriteLine($"        Exception: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"        Inner: {ex.InnerException.Message}");
        }
    }
} 