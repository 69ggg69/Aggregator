using Microsoft.Extensions.Logging;
using System.Net.Http;
using Aggregator.Data;
using Aggregator.Services;

namespace Aggregator.ParserServices
{
    public class AskStudioParser : BaseParser
    {
        public AskStudioParser(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<AskStudioParser> logger,
            ImageService imageService) 
            : base(context, clientFactory, logger, imageService)
        {
        }

        public override string ShopName => "Ask Studio";
        protected override string BaseUrl => "https://askstudio.ru/shop/";
        protected override string ProductSelector => "//div[contains(@class,'catalog-list__item')]";
        protected override string NameSelector => ".//a[contains(@class,'card-product__title')]";
        protected override string PriceSelector => ".//div[contains(@class,'product-price__price-current')]";
        protected override string ImageSelector => ".//span[contains(@class,'card-product__image')]";
    }
}