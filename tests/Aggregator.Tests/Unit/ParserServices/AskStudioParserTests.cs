using System.Threading.Tasks;
using Aggregator.ParserServices;
using Aggregator.Services;
using Aggregator.Tests.Fixtures;
using Aggregator.Tests.Helpers;
using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aggregator.Tests.Unit.ParserServices;

/// <summary>
/// Unit тесты для AskStudioParser
/// Тестирует парсинг HTML без использования реальной БД или HTTP запросов
/// </summary>
public class AskStudioParserTests : IDisposable
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly ILogger<AskStudioParser> _testsLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpFactory;
    private readonly ImageService _imageService;
    private readonly MockedAskStudioParser _mockedAskParser;

    public AskStudioParserTests()
    {
        // Настраиваем тестовое окружение
        _databaseFixture = new DatabaseFixture();
        _mockHttpFactory = new Mock<IHttpClientFactory>();
        _testsLogger = TestLogger.Create<AskStudioParser>();

        // Создаем мок ImageService с правильными параметрами
        var mockImageHttpFactory = new Mock<IHttpClientFactory>();
        var mockImageLogger = new Mock<ILogger<ImageService>>();
        _imageService = new ImageService(mockImageHttpFactory.Object, mockImageLogger.Object);

        // Создаем экземпляр моковой версии парсера
        _mockedAskParser = new MockedAskStudioParser(
            _mockHttpFactory.Object,
            _testsLogger,
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
        products.Should().HaveCountGreaterThan(0);
        products.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name));
    }

    /// <summary>
    /// Пример теста с РЕАЛЬНЫМ логгером - логи будут видны в консоли
    /// </summary>
    [Fact]
    public async Task ParseProducts_ShouldParseFirstProduct()
    {
        _testsLogger.LogInformation("🧪 Начинаем тест с реальным логгером 2");


        // Act
        var products = await _mockedAskParser.ParseProducts();

        {
            // Show first product name and price
            var firstProduct = products.First();
            _testsLogger.LogInformation("Первый товар: {name}, цена: {price}", firstProduct.Name, firstProduct.Price);
            firstProduct.Name.Should().Be("Cумка Tub Butter Mini");
            firstProduct.Price.Should().Be("13500");
        }

        // Assert
        products.Should().NotBeNull();

        products.Should().HaveCountGreaterThan(0);
    }

    /// <summary>
    /// САМЫЙ ПРОСТОЙ способ видеть логи - используем статические методы Log
    /// </summary>
    [Fact]
    public async Task ParseProducts_ShouldParseManyProducts()
    {
        // Arrange
        var products = await _mockedAskParser.ParseProducts();

        // Act & Assert
        products.Should().NotBeNull();


        products.Should().HaveCountGreaterThan(0);
        _testsLogger.LogInformation("✅ Тест прошел успешно!");
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
        GC.SuppressFinalize(this);
    }
}