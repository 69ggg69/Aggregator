using System.Collections.Generic;
using System.Threading.Tasks;
using Aggregator.Models;

namespace Aggregator.Interfaces
{
    /// <summary>
    /// Интерфейс парсера для двухэтапного парсинга товаров
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Название магазина
        /// </summary>
        string ShopName { get; }

        /// <summary>
        /// URL магазина
        /// </summary>
        string ShopUrl { get; }
        
        /// <summary>
        /// Этап 1: Парсинг базовой информации о товарах
        /// Извлекает название товара и ссылку на страницу товара
        /// </summary>
        /// <returns>Список товаров с базовой информацией</returns>
        Task<List<Product>> ParseBasicProductsAsync();

        /// <summary>
        /// Этап 2: Парсинг детальной информации о конкретном товаре
        /// Извлекает описание, материал, варианты, изображения и другую детальную информацию
        /// </summary>
        /// <param name="product">Товар с базовой информацией и ссылкой</param>
        /// <returns>Обновленный товар с детальной информацией</returns>
        Task<Product> ParseDetailedProductAsync(Product product);
    }
}