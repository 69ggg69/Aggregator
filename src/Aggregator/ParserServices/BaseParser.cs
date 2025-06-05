using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
using Aggregator.Services;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Базовый абстрактный класс для парсеров интернет-магазинов.
    /// Предоставляет общую логику для парсинга товаров с веб-сайтов.
    /// </summary>
    /// <remarks>
    /// Класс содержит общую логику для:
    /// - Парсинга HTML страниц с товарами
    /// - Извлечения информации о товарах (название, цена, изображения)
    /// - Сохранения товаров в базу данных
    /// - Загрузки и сохранения изображений товаров
    /// 
    /// Наследники должны определить специфичные для сайта селекторы и базовый URL.
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр базового парсера
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="clientFactory">Фабрика HTTP клиентов</param>
    /// <param name="logger">Логгер</param>
    /// <param name="imageService">Сервис для работы с изображениями</param>
    public abstract class BaseParser(
        ApplicationDbContext context,
        IHttpClientFactory clientFactory,
        ILogger logger,
        ImageService imageService) : IParser
    {
        /// <summary>
        /// Контекст базы данных для работы с товарами
        /// </summary>
        protected readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Фабрика HTTP клиентов для выполнения веб-запросов
        /// </summary>
        protected readonly IHttpClientFactory _clientFactory = clientFactory;

        /// <summary>
        /// Логгер для записи информации о работе парсера
        /// </summary>
        protected readonly ILogger _logger = logger;

        /// <summary>
        /// Сервис для загрузки и сохранения изображений товаров
        /// </summary>
        protected readonly ImageService _imageService = imageService;

        /// <summary>
        /// Получает название магазина. Должно быть реализовано в наследниках.
        /// </summary>
        /// <value>Уникальное название магазина</value>
        public abstract string ShopName { get; }

        /// <summary>
        /// Получает базовый URL для парсинга. Должен быть реализован в наследниках.
        /// </summary>
        /// <value>URL страницы с товарами для парсинга</value>
        protected abstract string BaseUrl { get; }

        /// <summary>
        /// Получает XPath селектор для контейнеров товаров. Должен быть реализован в наследниках.
        /// </summary>
        /// <value>XPath селектор, который выбирает все элементы товаров на странице</value>
        protected abstract string ProductSelector { get; }

        /// <summary>
        /// Получает XPath селектор для названия товара. Должен быть реализован в наследниках.
        /// </summary>
        /// <value>XPath селектор относительно контейнера товара для извлечения названия</value>
        protected abstract string NameSelector { get; }

        /// <summary>
        /// Получает XPath селектор для цены товара. Должен быть реализован в наследниках.
        /// </summary>
        /// <value>XPath селектор относительно контейнера товара для извлечения цены</value>
        protected abstract string PriceSelector { get; }

        /// <summary>
        /// Получает XPath селектор для изображения товара. Должен быть реализован в наследниках.
        /// </summary>
        /// <value>XPath селектор относительно контейнера товара для извлечения изображения</value>
        protected abstract string ImageSelector { get; }

        /// <summary>
        /// Извлекает URL изображения из HTML узла товара
        /// </summary>
        /// <param name="node">HTML узел, содержащий информацию о товаре</param>
        /// <returns>URL изображения или пустая строка, если изображение не найдено</returns>
        /// <remarks>
        /// Метод пытается найти изображение в следующем порядке:
        /// 1. Атрибут src у тега img
        /// 2. URL из CSS background-image в атрибуте style
        /// 
        /// Поддерживает как абсолютные, так и относительные URL.
        /// </remarks>
        protected virtual string? ExtractImageUrl(HtmlNode node)
        {
            var imageNode = node.SelectSingleNode(ImageSelector);
            if (imageNode == null) return null;

            // Для тега img с атрибутом src
            var src = imageNode.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src))
            {
                return NormalizeImageUrl(src);
            }

            // Для элементов с background-image в style
            var style = imageNode.GetAttributeValue("style", "");
            if (!string.IsNullOrEmpty(style))
            {
                var urlMatch = System.Text.RegularExpressions.Regex.Match(style, @"url\(([^)]+)\)");
                if (urlMatch.Success)
                {
                    // TODO: why [1] ?
                    var url = urlMatch.Groups[1].Value.Trim('"', '\'');
                    return NormalizeImageUrl(url);
                }
            }

            return null;
        }

        /// <summary>
        /// Нормализует URL изображения, преобразуя относительные пути в абсолютные
        /// </summary>
        /// <param name="url">Исходный URL изображения</param>
        /// <returns>Нормализованный абсолютный URL</returns>
        /// <remarks>
        /// Если URL начинается с "/", добавляется базовый домен сайта.
        /// Абсолютные URL возвращаются без изменений.
        /// </remarks>
        protected virtual string NormalizeImageUrl(string url)
        // TODO: change in mocked class
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            // Если URL относительный, добавляем базовый домен
            if (url.StartsWith('/'))
            {
                var baseUri = new Uri(BaseUrl);
                return $"{baseUri.Scheme}://{baseUri.Host}{url}";
            }

            return url;
        }

        /// <summary>
        /// Парсит товары с веб-страницы магазина
        /// </summary>
        /// <returns>Список найденных товаров</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках загрузки или парсинга страницы</exception>
        /// <remarks>
        /// Метод выполняет следующие действия:
        /// 1. Загружает HTML страницу по BaseUrl
        /// 2. Находит все элементы товаров используя ProductSelector
        /// 3. Для каждого товара извлекает название, цену и изображение
        /// 4. Загружает и сохраняет изображения через ImageService
        /// 5. Исключает дубликаты товаров по комбинации "название + цена"
        /// 6. Возвращает список уникальных товаров
        /// 
        /// В случае ошибки возвращает пустой список и логирует ошибку.
        /// </remarks>
        public async Task<List<Product>> ParseProducts()
        {
            var products = new List<Product>();
            var web = new HtmlWeb();

            try
            {
                var doc = await web.LoadFromWebAsync(BaseUrl);
                var productNodes = doc.DocumentNode.SelectNodes(ProductSelector);

                var uniqueProducts = new HashSet<string>();

                if (productNodes != null)
                {
                    foreach (var node in productNodes)
                    {
                        var name = node.SelectSingleNode(NameSelector)?.InnerText.Trim();
                        var price = node.SelectSingleNode(PriceSelector)?.InnerText.Trim();
                        var imageUrl = ExtractImageUrl(node);

                        if (!string.IsNullOrEmpty(name))
                        {
                            price = price?
                                .Replace("&nbsp;", " ")
                                .Replace("РУБ", "")
                                .Replace("руб", "")
                                .Replace("₽", "")
                                .Trim();

                            var productKey = $"{name}_{(string.IsNullOrEmpty(price) ? "PRICEERROR" : price)}";

                            if (!uniqueProducts.Add(productKey))
                            {
                                // Загружаем и сохраняем изображение
                                string? localImagePath = null;
                                if (!string.IsNullOrEmpty(imageUrl))
                                {
                                    localImagePath = await _imageService.DownloadAndSaveImageAsync(imageUrl, ShopName);
                                }

                                products.Add(new Product
                                {
                                    Name = name,
                                    Shop = ShopName,
                                    ParseDate = DateTime.UtcNow,
                                    Price = price,
                                    ImageUrl = imageUrl,
                                    LocalImagePath = localImagePath
                                });
                            }
                            else
                            {
                                _logger.LogInformation("Пропущен дубликат товара: {name} - {price}", name, price);
                            }
                        }
                    }
                }

                _logger.LogInformation("Найдено {productsCount} уникальных товаров из {productNodesCount} элементов", products.Count, productNodes?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке страницы {ShopName}", ShopName);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутренняя ошибка");
                }
                return [];
            }

            return products;
        }

        /// <summary>
        /// Выполняет полный цикл парсинга: извлекает товары и сохраняет новые в базу данных
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию парсинга</returns>
        /// <exception cref="Exception">Выбрасывается при критических ошибках парсинга или сохранения</exception>
        /// <remarks>
        /// Метод выполняет следующие действия:
        /// 1. Парсит товары с сайта используя ParseProducts()
        /// 2. Загружает существующие товары из БД за текущий день
        /// 3. Фильтрует только новые товары (не найденные в БД)
        /// 4. Сохраняет новые товары в базу данных
        /// 5. Логирует результаты операции
        /// 
        /// Товары считаются дубликатами, если совпадают название и цена.
        /// </remarks>
        public async Task ParseAsync()
        {
            try
            {
                var client = _clientFactory.CreateClient("SafeHttpClient");
                var products = await ParseProducts();

                var existingProducts = await _context.Products
                    .Where(p => p.Shop == ShopName && p.ParseDate.Date == DateTime.UtcNow.Date)
                    .ToListAsync();

                var newProducts = products
                    .Where(p => !existingProducts.Any(ep =>
                        ep.Name == p.Name &&
                        ep.Price == p.Price))
                    .ToList();

                if (newProducts.Count > 0)
                {
                    await _context.Products.AddRangeAsync(newProducts);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Добавлено {newProductsCount} новых товаров", newProducts.Count);
                }
                else
                {
                    _logger.LogInformation("Новых товаров не обнаружено");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге {ShopName}", ShopName);
                throw;
            }
        }
    }
}