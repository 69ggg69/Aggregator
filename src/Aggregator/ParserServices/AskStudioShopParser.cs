using Microsoft.Extensions.Logging;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Парсер для магазина AskStudio (askstudio.ru)
    /// </summary>
    public class AskStudioShopParser(IHttpClientFactory clientFactory, ILogger<AskStudioShopParser> logger) : ShopParser(clientFactory, logger)
    {
        public override string ShopName => "AskStudio";

        public override ShopUrlConfig[] BaseUrls => new[]
        {
            new ShopUrlConfig
            {
                BaseUrl = "https://askstudio.ru/shop/?page=30",
                PaginationRules = new string[0] // Нет пагинации - все товары на одной длинной странице  
            }
        };

        // Селекторы нужно определить на основе реальной структуры HTML
        // Пока делаем предположение на основе типичной структуры каталогов
        protected override string ProductSelector => "//div[contains(@class,'catalog-list__item')]";

        protected override string ProductNameSelector => ".//a[contains(@class,'card-product__title')]";

        protected override string ProductLinkSelector => ".//a[contains(@class,'card-product__title')]";

    }
}