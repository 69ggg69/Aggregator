using Microsoft.Extensions.Logging;
using System.Net.Http;
using Aggregator.Data;

namespace Aggregator.ParserServices
{
    public class AskStudioParser : BaseParser
    {
        public AskStudioParser(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<AskStudioParser> logger) 
            : base(context, clientFactory, logger)
        {
        }

        public override string ShopName => "Ask Studio";
        protected override string BaseUrl => "https://askstudio.ru/shop/";
        protected override string ProductSelector => "//div[contains(@class,'catalog-list__item')]";
        protected override string NameSelector => ".//a[contains(@class,'card-product__title')]";
        protected override string PriceSelector => ".//div[contains(@class,'product-price__price-current')]";
    }
}