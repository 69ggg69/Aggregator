using Microsoft.Extensions.Logging;
using System.Net.Http;
using Aggregator.Data;
using Aggregator.Services;

namespace Aggregator.ParserServices
{
    public class ZnwrParser : BaseParser
    {
        public ZnwrParser(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<ZnwrParser> logger,
            ImageService imageService)
            : base(context, clientFactory, logger, imageService)
        {
        }

        public override string ShopName => "ZNWR";
        protected override string BaseUrl => "https://znwr.ru/catalog/woman/";
        protected override string ProductSelector => "//div[contains(@class,'card')]";
        protected override string NameSelector => ".//div[contains(@class,'card__product-name')]";
        protected override string PriceSelector => ".//span[contains(@class,'card__price-final')]";
        protected override string ImageSelector => ".//img[contains(@class,'swiper-lazy')]";
    }
}