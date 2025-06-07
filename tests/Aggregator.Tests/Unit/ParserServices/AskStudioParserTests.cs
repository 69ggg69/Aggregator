using Aggregator.ParserServices;
using Aggregator.Tests.Fixtures;
using Aggregator.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Aggregator.Services;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace Aggregator.Tests.Unit.ParserServices;

/// <summary>
/// Unit тесты для AskStudioParser
/// Тестирует парсинг HTML без использования реальной БД или HTTP запросов
/// </summary>
public class AskStudioParserTests : IDisposable
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly Mock<IHttpClientFactory> _mockHttpFactory;
    private readonly Mock<ILogger<AskStudioParser>> _mockLogger;
    private readonly ImageService _imageService;
    private readonly MockedAskStudioParser _mockedAskParser;

    public AskStudioParserTests()
    {
        // Настраиваем тестовое окружение
        _databaseFixture = new DatabaseFixture();
        _mockHttpFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<AskStudioParser>>();

        // Создаем мок ImageService с правильными параметрами
        var mockImageHttpFactory = new Mock<IHttpClientFactory>();
        var mockImageLogger = new Mock<ILogger<ImageService>>();
        _imageService = new ImageService(mockImageHttpFactory.Object, mockImageLogger.Object);

        // Создаем экземпляр моковой версии парсера
        _mockedAskParser = new MockedAskStudioParser(
            _databaseFixture.Context,
            _mockHttpFactory.Object,
            _mockLogger.Object,
            _imageService
        );
    }

    [Fact]
    public void ShopName_ShouldReturnCorrectValue()
    {
        // Act
        var shopName = _mockedAskParser.ShopName;

        // Assert
        shopName.Should().Be("Ask Studio");
    }

    [Fact]
    public void MockedParser_ShouldHaveFileBasedUrl()
    {
        // Act
        var fileBasedUrl = _mockedAskParser.GetBaseUrl();

        // Assert
        fileBasedUrl.Should().Be("file://" + TestDataHelper.GetTestFilePath("HtmlPages/askstudio/main_shop_page.html"));
    }

    [Fact]
    public void Parser_Selectors_ShouldHaveProductSelector()
    // TODO: add test for other selectors
    {
        // Используем рефлексию для проверки protected свойств (или создаем тестовый метод в парсере)
        // Для простоты проверяем, что объект создался корректно

        // Assert
        _mockedAskParser.GetProductSelector().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseProducts_WithRealHtmlData_ShouldExtractProductsNames()
    {
        // Arrange
        var products = await _mockedAskParser.ParseProducts();

        // Assert
        products.Should().NotBeNull();
        // _mockLogger.
        Console.WriteLine("Products count: " + products.Count);
        products.Should().HaveCountGreaterThan(0);
        products.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name));
    }

    /// <summary>
    /// Пример теста с РЕАЛЬНЫМ логгером - логи будут видны в консоли
    /// </summary>
    [Fact]
    public async Task ParseProducts_WithRealLogger_ShouldShowLogsInConsole()
    {
        // Arrange - создаем реальный логгер
        var realLogger = TestLogger.Create<AskStudioParser>();
        
        var parserWithRealLogger = new MockedAskStudioParser(
            _databaseFixture.Context,
            _mockHttpFactory.Object,
            realLogger, // РЕАЛЬНЫЙ логгер вместо мока!
            _imageService
        );

        realLogger.LogInformation("🧪 Начинаем тест с реальным логгером");

        // Act
        var products = await parserWithRealLogger.ParseProducts();

        // Assert
        products.Should().NotBeNull();
        realLogger.LogInformation("✅ Найдено товаров: {ProductCount}", products.Count);
        
        products.Should().HaveCountGreaterThan(0);
        realLogger.LogInformation("🎉 Тест успешно завершен!");
    }

    /// <summary>
    /// САМЫЙ ПРОСТОЙ способ видеть логи - используем статические методы Log
    /// </summary>
    [Fact]
    public async Task ParseProducts_WithSimpleLogging_SuperEasy()
    {
        Log.Info("🚀 Начинаем самый простой тест с логами");
        
        // Arrange
        Log.Debug("Настраиваем тестовые данные...");
        var products = await _mockedAskParser.ParseProducts();

        // Act & Assert
        products.Should().NotBeNull();
        Log.Info("Найдено товаров: {0}", products.Count);
        
        if (products.Count > 0)
        {
            Log.Info("Первый товар: {0}", products[0].Name);
            Log.Info("Цена первого товара: {0}", products[0].Price);
        }

        products.Should().HaveCountGreaterThan(0);
        Log.Info("✅ Тест прошел успешно!");
    }

    [Fact]
    public void MockedParser_ShouldUseLocalHtmlFiles()
    {
        // Arrange & Act
        // Проверяем, что моковый парсер правильно настроен на использование локальных файлов

        // Assert
        _mockedAskParser.Should().NotBeNull();
        _mockedAskParser.ShopName.Should().Be("Ask Studio");

        // Проверяем, что тестовый HTML файл существует
        TestDataHelper.TestFileExists("HtmlPages/askstudio/main_shop_page.html")
            .Should().BeTrue("Тестовый HTML файл должен существовать");
    }

    public void Dispose()
    {
        _databaseFixture.Dispose();
    }
}