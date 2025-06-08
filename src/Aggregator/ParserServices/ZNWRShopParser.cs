using Microsoft.Extensions.Logging;

namespace Aggregator.ParserServices
{
    /// <summary>
    /// Парсер для магазина ZNWR (znwr.ru)
    /// </summary>
    public class ZNWRShopParser(IHttpClientFactory clientFactory, ILogger<ZNWRShopParser> logger) : ShopParser(clientFactory, logger)
    {
        public override string ShopName => "ZNWR";

        public override ShopUrlConfig[] BaseUrls =>
        [
            new ShopUrlConfig
            {
                BaseUrl = "https://znwr.ru/catalog/woman/",
                PaginationRules = new string[0] // Нет пагинации - все товары на одной длинной странице  
            },
            new ShopUrlConfig
            {
                BaseUrl = "https://znwr.ru/catalog/man/",
                PaginationRules = new string[0] // Нет пагинации - все товары на одной длинной странице  
            }
        ];

        protected override string ProductSelector => "//div[contains(@class,'card')]";

        protected override string ProductNameSelector => ".//div[contains(@class,'card__product-name')]";

        protected override string ProductLinkSelector => ".//a[contains(@class,'card__product-name')]";

    }
}