using Aggregator.Models;

namespace Aggregator.Interfaces;

/// <summary>
/// Интерфейс для работы с товарами в базе данных
/// Отделяет логику парсинга от логики работы с БД
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Получает существующие товары магазина за указанную дату
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <returns>Список товаров</returns>
    Task<List<Product>> GetProductsByShopAsync(string shopName);

    /// <summary>
    /// Добавляет новые товары в базу данных
    /// </summary>
    /// <param name="products">Список товаров для добавления</param>
    /// <returns>Количество добавленных товаров</returns>
    Task<int> AddProductsAsync(List<Product> products);

    /// <summary>
    /// Проверяет существует ли товар с таким названием и ценой
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <param name="productName">Название товара</param>
    /// <param name="price">Цена товара</param>
    /// <returns>True если товар существует</returns>
    Task<bool> ProductExistsAsync(string shopName, string productName, string? price);

    /// <summary>
    /// Получает статистику товаров по магазинам
    /// </summary>
    /// <returns>Список статистики по магазинам</returns>
    Task<List<ShopStatistics>> GetShopStatisticsAsync();

    /// <summary>
    /// Получает общее количество товаров в БД
    /// </summary>
    /// <returns>Количество товаров</returns>
    Task<int> GetTotalProductsCountAsync();

    /// <summary>
    /// Получает список URL всех товаров магазина (для проверки дубликатов)
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <returns>Список URL товаров</returns>
    Task<HashSet<string>> GetProductUrlsByShopAsync(string shopName);

    /// <summary>
    /// Добавляет один товар в базу данных в отдельной транзакции
    /// </summary>
    /// <param name="product">Товар для добавления</param>
    /// <returns>True если товар успешно добавлен</returns>
    Task<bool> AddProductAsync(Product product);

    /// <summary>
    /// Получает магазин по названию или создает его, если не существует
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <param name="shopUrl">URL магазина (опционально)</param>
    /// <returns>Магазин</returns>
    Task<Shop> EnsureShopExistsAsync(string shopName, string? shopUrl = null);

    /// <summary>
    /// Получает ID магазина по названию
    /// </summary>
    /// <param name="shopName">Название магазина</param>
    /// <returns>ID магазина или null если не найден</returns>
    Task<int?> GetShopIdByNameAsync(string shopName);
} 