using Microsoft.Extensions.Logging;
using System.Net.Http;
using Aggregator.Data;
using Aggregator.Services;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Парсер для интернет-магазина Ask Studio (https://askstudio.ru/)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ask Studio - российский интернет-магазин аксессуаров и подарков.
    /// Специализируется на продаже сумок, украшений, аксессуаров и других товаров.
    /// </para>
    /// 
    /// <para>
    /// <strong>Структура парсинга:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Базовый URL: https://askstudio.ru/shop/</description></item>
    /// <item><description>Контейнер товара: div с классом 'catalog-list__item'</description></item>
    /// <item><description>Название товара: ссылка с классом 'card-product__title'</description></item>
    /// <item><description>Цена товара: div с классом 'product-price__price-current'</description></item>
    /// <item><description>Изображение товара: span с классом 'card-product__image'</description></item>
    /// </list>
    /// 
    /// <para>
    /// <strong>Особенности сайта:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Использует WooCommerce в качестве платформы</description></item>
    /// <item><description>Поддерживает пагинацию товаров</description></item>
    /// <item><description>Изображения могут быть в формате background-image</description></item>
    /// <item><description>Цены указаны в рублях с символом ₽</description></item>
    /// </list>
    /// 
    /// <para>
    /// <strong>Пример использования:</strong>
    /// </para>
    /// <code>
    /// var parser = new AskStudioParser(context, httpFactory, logger, imageService);
    /// var products = await parser.ParseProducts();
    /// await parser.ParseAsync(); // Полный цикл с сохранением в БД
    /// </code>
    /// </remarks>
    /// <seealso cref="BaseParser"/>
    /// <seealso cref="ZnwrParser"/>
    public class AskStudioParser : BaseParser
    {
        /// <summary>
        /// Инициализирует новый экземпляр парсера Ask Studio
        /// </summary>
        /// <param name="context">Контекст базы данных для сохранения товаров</param>
        /// <param name="clientFactory">Фабрика HTTP клиентов для выполнения запросов к сайту</param>
        /// <param name="logger">Логгер для записи информации о работе парсера</param>
        /// <param name="imageService">Сервис для загрузки и сохранения изображений товаров</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если любой из параметров равен null</exception>
        public AskStudioParser(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<AskStudioParser> logger,
            ImageService imageService) 
            : base(context, clientFactory, logger, imageService)
        {
        }

        /// <summary>
        /// Получает название магазина "Ask Studio"
        /// </summary>
        /// <value>Строка "Ask Studio" - уникальный идентификатор магазина в системе</value>
        /// <remarks>
        /// Это значение используется для:
        /// - Идентификации товаров в базе данных
        /// - Генерации имен файлов изображений
        /// - Логирования операций парсинга
        /// </remarks>
        public override string ShopName => "Ask Studio";
        
        /// <summary>
        /// Получает базовый URL для парсинга товаров Ask Studio
        /// </summary>
        /// <value>https://askstudio.ru/shop/ - основная страница каталога товаров</value>
        /// <remarks>
        /// Эта страница содержит список всех доступных товаров магазина.
        /// Поддерживает пагинацию через параметр ?page=N
        /// </remarks>
        protected override string BaseUrl => "https://askstudio.ru/shop/";
        
        /// <summary>
        /// Получает XPath селектор для поиска контейнеров товаров на странице
        /// </summary>
        /// <value>//div[contains(@class,'catalog-list__item')]</value>
        /// <remarks>
        /// Селектор находит все div элементы, которые содержат класс 'catalog-list__item'.
        /// Каждый такой элемент представляет отдельный товар на странице.
        /// Обычно на странице отображается 20-30 товаров.
        /// </remarks>
        protected override string ProductSelector => "//div[contains(@class,'catalog-list__item')]";
        
        /// <summary>
        /// Получает XPath селектор для извлечения названия товара
        /// </summary>
        /// <value>.//a[contains(@class,'card-product__title')]</value>
        /// <remarks>
        /// Селектор ищет ссылку с классом 'card-product__title' внутри контейнера товара.
        /// Название товара находится в текстовом содержимом (InnerText) этого элемента.
        /// Обычно название также является ссылкой на страницу товара.
        /// </remarks>
        protected override string NameSelector => ".//a[contains(@class,'card-product__title')]";
        
        /// <summary>
        /// Получает XPath селектор для извлечения цены товара
        /// </summary>
        /// <value>.//div[contains(@class,'product-price__price-current')]</value>
        /// <remarks>
        /// Селектор находит div с классом 'product-price__price-current' - текущую цену товара.
        /// Цена может содержать символы валюты (₽, руб, РУБ) и пробелы, которые автоматически удаляются.
        /// Некоторые товары могут иметь скидку, в таком случае это будет цена со скидкой.
        /// </remarks>
        protected override string PriceSelector => ".//div[contains(@class,'product-price__price-current')]";
        
        /// <summary>
        /// Получает XPath селектор для извлечения изображения товара
        /// </summary>
        /// <value>.//span[contains(@class,'card-product__image')]</value>
        /// <remarks>
        /// Селектор находит span с классом 'card-product__image', который содержит изображение товара.
        /// Изображение может быть:
        /// - В виде вложенного тега img с атрибутом src
        /// - В виде CSS background-image в атрибуте style
        /// 
        /// Метод ExtractImageUrl автоматически обрабатывает оба варианта.
        /// </remarks>
        protected override string ImageSelector => ".//span[contains(@class,'card-product__image')]";
    }
}