using System.Collections.Generic;
using System.Threading.Tasks;
using Aggregator.Models;

namespace Aggregator.Interfaces
{
    /// <summary>
    /// Интерфейс парсера товаров с веб-сайтов
    /// Отвечает только за извлечение данных, без работы с БД
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Название магазина
        /// </summary>
        string ShopName { get; }
        
        /// <summary>
        /// Парсит товары с сайта и возвращает их список
        /// НЕ сохраняет в базу данных
        /// </summary>
        /// <returns>Список распарсенных товаров</returns>
        Task<List<Product>> ParseProducts();
    }
}