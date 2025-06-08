using Aggregator.Interfaces;
using Aggregator.Models;
using Moq;

namespace Aggregator.Tests.Helpers;

/// <summary>
/// Результат создания мока DatabaseService
/// Содержит как основной мок службы, так и мок репозитория для проверки вызовов
/// </summary>
public class DatabaseServiceMockResult
{
    public Mock<IDatabaseService> DatabaseService { get; init; } = null!;
    public Mock<IProductRepository> ProductRepository { get; init; } = null!;
}

/// <summary>
/// Хелпер для создания моков DatabaseService в тестах
/// Упрощает настройку моков для различных сценариев тестирования
/// </summary>
public static class DatabaseServiceMock
{
    /// <summary>
    /// Создает стандартный мок DatabaseService с настройками по умолчанию
    /// </summary>
    /// <returns>Результат с настроенными моками</returns>
    public static DatabaseServiceMockResult CreateDefault()
    {
        var mockDatabaseService = new Mock<IDatabaseService>();
        var mockProductRepo = new Mock<IProductRepository>();

        // Настройки по умолчанию - БД пуста, все операции успешны
        mockProductRepo.Setup(x => x.GetProductsByShopAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Product>());

        mockProductRepo.Setup(x => x.AddProductsAsync(It.IsAny<List<Product>>()))
            .ReturnsAsync((List<Product> products) => products.Count);

        mockProductRepo.Setup(x => x.ProductExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        mockProductRepo.Setup(x => x.GetTotalProductsCountAsync())
            .ReturnsAsync(0);

        mockProductRepo.Setup(x => x.GetShopStatisticsAsync())
            .ReturnsAsync(new List<ShopStatistics>());

        mockDatabaseService.Setup(x => x.Products).Returns(mockProductRepo.Object);

        // Настройка основных методов DatabaseService
        mockDatabaseService.Setup(x => x.CheckConnectionAsync()).ReturnsAsync(true);
        mockDatabaseService.Setup(x => x.MigrateDatabaseAsync()).ReturnsAsync(true);
        mockDatabaseService.Setup(x => x.CreateBackupAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new DatabaseServiceMockResult
        {
            DatabaseService = mockDatabaseService,
            ProductRepository = mockProductRepo
        };
    }

    /// <summary>
    /// Создает мок с существующими товарами в БД
    /// </summary>
    /// <param name="existingProducts">Список существующих товаров</param>
    /// <returns>Результат с предзаполненными данными</returns>
    public static DatabaseServiceMockResult CreateWithExistingProducts(List<Product> existingProducts)
    {
        var result = CreateDefault();
        var mockProductRepo = new Mock<IProductRepository>();

        // Возвращаем существующие товары
        mockProductRepo.Setup(x => x.GetProductsByShopAsync(It.IsAny<string>()))
            .ReturnsAsync((string shopName) => 
                existingProducts.Where(p => p.Shop == shopName).ToList());

        mockProductRepo.Setup(x => x.ProductExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string shopName, string productName, string price) =>
                existingProducts.Any(p => p.Shop == shopName && p.Name == productName && p.Price == price));

        mockProductRepo.Setup(x => x.AddProductsAsync(It.IsAny<List<Product>>()))
            .ReturnsAsync((List<Product> products) => products.Count);

        mockProductRepo.Setup(x => x.GetTotalProductsCountAsync())
            .ReturnsAsync(existingProducts.Count);

        result.DatabaseService.Setup(x => x.Products).Returns(mockProductRepo.Object);

        return new DatabaseServiceMockResult
        {
            DatabaseService = result.DatabaseService,
            ProductRepository = mockProductRepo
        };
    }

    /// <summary>
    /// Создает мок который симулирует ошибки БД
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке</param>
    /// <returns>Результат с моком который бросает исключения</returns>
    public static DatabaseServiceMockResult CreateWithDatabaseError(string errorMessage = "Database connection failed")
    {
        var result = CreateDefault();
        var mockProductRepo = new Mock<IProductRepository>();

        // Все операции бросают исключения
        mockProductRepo.Setup(x => x.GetProductsByShopAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        mockProductRepo.Setup(x => x.AddProductsAsync(It.IsAny<List<Product>>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        mockProductRepo.Setup(x => x.ProductExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        result.DatabaseService.Setup(x => x.Products).Returns(mockProductRepo.Object);
        result.DatabaseService.Setup(x => x.CheckConnectionAsync()).ReturnsAsync(false);

        return new DatabaseServiceMockResult
        {
            DatabaseService = result.DatabaseService,
            ProductRepository = mockProductRepo
        };
    }

    /// <summary>
    /// Создает мок для конкретного магазина с настройкой дубликатов
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <param name="existingProductsCount">Количество существующих товаров</param>
    /// <param name="allowDuplicates">Разрешить дубликаты</param>
    /// <returns>Настроенный результат</returns>
    public static DatabaseServiceMockResult CreateForShop(string shopName, int existingProductsCount = 0, bool allowDuplicates = false)
    {
        var result = CreateDefault();
        var mockProductRepo = new Mock<IProductRepository>();

        // Генерируем существующие товары
        var existingProducts = Enumerable.Range(1, existingProductsCount)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Existing Product {i}",
                Price = $"{100 * i}",
                Shop = shopName,
                ParseDate = DateTime.UtcNow
            })
            .ToList();

        mockProductRepo.Setup(x => x.GetProductsByShopAsync(shopName))
            .ReturnsAsync(existingProducts);

        mockProductRepo.Setup(x => x.ProductExistsAsync(shopName, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string shop, string name, string price) =>
            {
                if (allowDuplicates) return false;
                return existingProducts.Any(p => p.Name == name && p.Price == price);
            });

        mockProductRepo.Setup(x => x.AddProductsAsync(It.IsAny<List<Product>>()))
            .ReturnsAsync((List<Product> products) => products.Count);

        result.DatabaseService.Setup(x => x.Products).Returns(mockProductRepo.Object);

        return new DatabaseServiceMockResult
        {
            DatabaseService = result.DatabaseService,
            ProductRepository = mockProductRepo
        };
    }

    /// <summary>
    /// Создает мок с детальной статистикой
    /// </summary>
    /// <param name="totalProducts">Общее количество товаров</param>
    /// <param name="shops">Статистика по магазинам</param>
    /// <returns>Результат с статистикой</returns>
    public static DatabaseServiceMockResult CreateWithStatistics(int totalProducts, params (string shopName, int productCount)[] shops)
    {
        var result = CreateDefault();
        var mockProductRepo = new Mock<IProductRepository>();

        var statistics = shops.Select(s => new ShopStatistics
        {
            ShopName = s.shopName,
            ProductCount = s.productCount,
            LastUpdate = DateTime.UtcNow.AddHours(-1)
        }).ToList();

        mockProductRepo.Setup(x => x.GetTotalProductsCountAsync()).ReturnsAsync(totalProducts);
        mockProductRepo.Setup(x => x.GetShopStatisticsAsync()).ReturnsAsync(statistics);

        result.DatabaseService.Setup(x => x.Products).Returns(mockProductRepo.Object);

        return new DatabaseServiceMockResult
        {
            DatabaseService = result.DatabaseService,
            ProductRepository = mockProductRepo
        };
    }
} 