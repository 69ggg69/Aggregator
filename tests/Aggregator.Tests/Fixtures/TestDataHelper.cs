using System.Text.Json;

namespace Aggregator.Tests.Fixtures;

/// <summary>
/// Помощник для работы с тестовыми данными
/// Обеспечивает загрузку HTML файлов, JSON данных и создание временных файлов
/// </summary>
public static class TestDataHelper
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        "TestData"
    );

    /// <summary>
    /// Читает содержимое тестового файла
    /// </summary>
    /// <param name="relativePath">Относительный путь от папки TestData</param>
    /// <returns>Содержимое файла как строка</returns>
    public static string ReadTestFile(string relativePath)
    {
        var fullPath = Path.Combine(TestDataPath, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Test file not found: {fullPath}");
        
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Читает и десериализует JSON тестовые данные
    /// </summary>
    /// <typeparam name="T">Тип объекта для десериализации</typeparam>
    /// <param name="fileName">Имя JSON файла в папке ExpectedResults</param>
    /// <returns>Десериализованный объект</returns>
    public static T ReadJsonTestData<T>(string fileName)
    {
        var json = ReadTestFile($"ExpectedResults/{fileName}");
        return JsonSerializer.Deserialize<T>(json) 
            ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }

    /// <summary>
    /// Сохраняет тестовый HTML контент в файл
    /// </summary>
    /// <param name="content">HTML контент</param>
    /// <param name="fileName">Имя файла</param>
    public static void SaveTestHtml(string content, string fileName)
    {
        var fullPath = Path.Combine(TestDataPath, "HtmlPages", fileName);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null)
            Directory.CreateDirectory(directory);
        
        File.WriteAllText(fullPath, content);
    }

    /// <summary>
    /// Проверяет существование тестового файла
    /// </summary>
    /// <param name="relativePath">Относительный путь от папки TestData</param>
    /// <returns>true если файл существует</returns>
    public static bool TestFileExists(string relativePath)
    {
        var fullPath = Path.Combine(TestDataPath, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Получает полный путь к тестовому файлу
    /// </summary>
    /// <param name="relativePath">Относительный путь от папки TestData</param>
    /// <returns>Полный путь к файлу</returns>
    public static string GetTestFilePath(string relativePath)
    {
        return Path.Combine(TestDataPath, relativePath);
    }

    /// <summary>
    /// Создает временный файл для тестов
    /// </summary>
    /// <param name="content">Содержимое файла</param>
    /// <param name="extension">Расширение файла (с точкой)</param>
    /// <returns>Путь к созданному временному файлу</returns>
    public static string CreateTempTestFile(string content, string extension = ".html")
    {
        var tempFile = Path.GetTempFileName() + extension;
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
} 