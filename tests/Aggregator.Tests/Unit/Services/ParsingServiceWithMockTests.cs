using Aggregator.Interfaces;
using Aggregator.Models;
using Aggregator.Services;
using Aggregator.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aggregator.Tests.Unit.Services;

/// <summary>
/// Примеры тестов с использованием моков DatabaseService
/// Показывает как тестировать сохранение в БД без реальной базы данных
/// </summary>
public class ParsingServiceWithMockTests
{
    private readonly ILogger<ParsingService> _logger;

    public ParsingServiceWithMockTests()
    {
        _logger = TestLogger.Create<ParsingService>();
    }

    [Fact]
    public async Task ParseShopAsync_WithEmptyDatabase_ShouldSaveAllProducts()
    {
        // Arrange - создаем мок парсера который возвращает 3 товара
        var mockParser = new Mock<IParser>();
        mockParser.Setup(x => x.ShopName).Returns("Test Shop");
        mockParser.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>
        {
            new() { Name = "Product 1", Price = "100", Shop = "Test Shop", ParseDate = DateTime.UtcNow },
            new() { Name = "Product 2", Price = "200", Shop = "Test Shop", ParseDate = DateTime.UtcNow },
            new() { Name = "Product 3", Price = "300", Shop = "Test Shop", ParseDate = DateTime.UtcNow }
        });

        // Arrange - создаем мок пустой БД (нет существующих товаров)
        var mockDatabase = DatabaseServiceMock.CreateDefault();
        var parsingService = new ParsingService(mockDatabase.DatabaseService.Object, _logger);

        // Act - выполняем парсинг
        var result = await parsingService.ParseShopAsync(mockParser.Object);

        // Assert - проверяем результат
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ShopName.Should().Be("Test Shop");
        result.ParsedCount.Should().Be(3);
        result.AddedCount.Should().Be(3); // Все товары должны быть добавлены

        // Assert - проверяем что нужные методы вызывались
        mockDatabase.ProductRepository.Verify(x => x.GetProductsByShopAsync("Test Shop"), Times.Once);
        mockDatabase.ProductRepository.Verify(x => x.AddProductsAsync(It.Is<List<Product>>(p => p.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task ParseShopAsync_WithExistingProducts_ShouldOnlySaveNewOnes()
    {
        // Arrange - создаем существующие товары в БД
        var existingProducts = new List<Product>
        {
            new() { Name = "Product 1", Price = "100", Shop = "Test Shop", ParseDate = DateTime.UtcNow },
            new() { Name = "Product 2", Price = "200", Shop = "Test Shop", ParseDate = DateTime.UtcNow }
        };

        // Arrange - парсер возвращает 1 существующий + 2 новых товара
        var mockParser = new Mock<IParser>();
        mockParser.Setup(x => x.ShopName).Returns("Test Shop");
        mockParser.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>
        {
            new() { Name = "Product 1", Price = "100", Shop = "Test Shop", ParseDate = DateTime.UtcNow }, // Дубликат
            new() { Name = "Product 3", Price = "300", Shop = "Test Shop", ParseDate = DateTime.UtcNow }, // Новый
            new() { Name = "Product 4", Price = "400", Shop = "Test Shop", ParseDate = DateTime.UtcNow }  // Новый
        });

        // Arrange - мок БД с существующими товарами
        var mockDatabase = DatabaseServiceMock.CreateWithExistingProducts(existingProducts);
        var parsingService = new ParsingService(mockDatabase.DatabaseService.Object, _logger);

        // Act
        var result = await parsingService.ParseShopAsync(mockParser.Object);

        // Assert - должно быть добавлено только 2 новых товара
        result.Success.Should().BeTrue();
        result.ParsedCount.Should().Be(3); // Всего распарсили 3
        result.AddedCount.Should().Be(2);  // Добавили только 2 новых

        // Проверяем что AddProducts вызвался с 2 товарами (без дубликата)
        mockDatabase.ProductRepository.Verify(x => x.AddProductsAsync(It.Is<List<Product>>(p => p.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task ParseShopAsync_WhenDatabaseFails_ShouldReturnFailureResult()
    {
        // Arrange - парсер работает нормально
        var mockParser = new Mock<IParser>();
        mockParser.Setup(x => x.ShopName).Returns("Test Shop");
        mockParser.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>
        {
            new() { Name = "Product 1", Price = "100", Shop = "Test Shop" }
        });

        // Arrange - мок БД который бросает ошибки
        var mockDatabase = DatabaseServiceMock.CreateWithDatabaseError("Connection timeout");
        var parsingService = new ParsingService(mockDatabase.DatabaseService.Object, _logger);

        // Act
        var result = await parsingService.ParseShopAsync(mockParser.Object);

        // Assert - должен вернуть результат с ошибкой
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Connection timeout");
        result.AddedCount.Should().Be(0);

        Log.Info("✅ Тест обработки ошибок БД прошел успешно");
    }

    [Fact]
    public async Task ParseShopAsync_WhenParserReturnsEmpty_ShouldNotCallDatabase()
    {
        // Arrange - парсер не находит товаров
        var mockParser = new Mock<IParser>();
        mockParser.Setup(x => x.ShopName).Returns("Empty Shop");
        mockParser.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>()); // Пустой список

        var mockDatabase = DatabaseServiceMock.CreateDefault();
        var parsingService = new ParsingService(mockDatabase.DatabaseService.Object, _logger);

        // Act
        var result = await parsingService.ParseShopAsync(mockParser.Object);

        // Assert
        result.Success.Should().BeTrue();
        result.ParsedCount.Should().Be(0);
        result.AddedCount.Should().Be(0);

        // БД НЕ должна вызываться если товаров нет
        mockDatabase.ProductRepository.Verify(x => x.GetProductsByShopAsync(It.IsAny<string>()), Times.Never);
        mockDatabase.ProductRepository.Verify(x => x.AddProductsAsync(It.IsAny<List<Product>>()), Times.Never);

        Log.Info("✅ Тест с пустым результатом парсинга прошел");
    }

    [Fact]
    public async Task ParseMultipleShopsAsync_ShouldProcessAllShops()
    {
        // Arrange - создаем несколько парсеров
        var parser1 = new Mock<IParser>();
        parser1.Setup(x => x.ShopName).Returns("Shop 1");
        parser1.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>
        {
            new() { Name = "Product A", Price = "100", Shop = "Shop 1" },
            new() { Name = "Product B", Price = "200", Shop = "Shop 1" }
        });

        var parser2 = new Mock<IParser>();
        parser2.Setup(x => x.ShopName).Returns("Shop 2");
        parser2.Setup(x => x.ParseProducts()).ReturnsAsync(new List<Product>
        {
            new() { Name = "Product C", Price = "300", Shop = "Shop 2" }
        });

        var parsers = new[] { parser1.Object, parser2.Object };

        // Arrange - мок БД
        var mockDatabase = DatabaseServiceMock.CreateDefault();
        var parsingService = new ParsingService(mockDatabase.DatabaseService.Object, _logger);

        // Act
        var results = await parsingService.ParseMultipleShopsAsync(parsers);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        
        var shop1Result = results.First(r => r.ShopName == "Shop 1");
        var shop2Result = results.First(r => r.ShopName == "Shop 2");

        shop1Result.AddedCount.Should().Be(2);
        shop2Result.AddedCount.Should().Be(1);

        // Каждый магазин должен вызвать свои методы БД
        mockDatabase.ProductRepository.Verify(x => x.GetProductsByShopAsync("Shop 1"), Times.Once);
        mockDatabase.ProductRepository.Verify(x => x.GetProductsByShopAsync("Shop 2"), Times.Once);

        Log.Info("✅ Тест парсинга нескольких магазинов прошел");
    }

    [Fact]
    public async Task DatabaseServiceMock_ShouldProvideStatistics()
    {
        // Arrange - создаем мок с статистикой
        var mockDatabase = DatabaseServiceMock.CreateWithStatistics(
            totalProducts: 150,
            ("Shop A", 50),
            ("Shop B", 75),
            ("Shop C", 25)
        );

        // Act - получаем статистику
        var totalProducts = await mockDatabase.DatabaseService.Object.Products.GetTotalProductsCountAsync();
        var shopStats = await mockDatabase.DatabaseService.Object.Products.GetShopStatisticsAsync();

        // Assert
        totalProducts.Should().Be(150);
        shopStats.Should().HaveCount(3);
        shopStats.Should().Contain(s => s.ShopName == "Shop A" && s.ProductCount == 50);
        shopStats.Should().Contain(s => s.ShopName == "Shop B" && s.ProductCount == 75);
        shopStats.Should().Contain(s => s.ShopName == "Shop C" && s.ProductCount == 25);

        Log.Info("✅ Тест статистики мока БД прошел");
    }
} 