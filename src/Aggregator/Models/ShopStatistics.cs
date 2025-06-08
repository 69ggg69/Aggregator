namespace Aggregator.Models;

/// <summary>
/// Статистика по магазину
/// </summary>
public class ShopStatistics
{
    /// <summary>
    /// Название магазина
    /// </summary>
    public string ShopName { get; set; } = string.Empty;
    
    /// <summary>
    /// Количество товаров в магазине
    /// </summary>
    public int ProductCount { get; set; }
    
    /// <summary>
    /// Дата последнего обновления товаров
    /// </summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Общая статистика парсинга
/// </summary>
public class ParsingStatistics
{
    /// <summary>
    /// Общее количество товаров во всех магазинах
    /// </summary>
    public int TotalProducts { get; set; }
    
    /// <summary>
    /// Дата последнего парсинга
    /// </summary>
    public DateTime? LastParseDate { get; set; }
    
    /// <summary>
    /// Статистика по каждому магазину
    /// </summary>
    public List<ShopStatistics> ShopStatistics { get; set; } = new();
} 