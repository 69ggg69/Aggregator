using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aggregator.ParserServices
{
    public abstract class BaseParser : IParser
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IHttpClientFactory _clientFactory;
        protected readonly ILogger _logger;

        protected BaseParser(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public abstract string ShopName { get; }
        protected abstract string BaseUrl { get; }
        protected abstract string ProductSelector { get; }
        protected abstract string NameSelector { get; }
        protected abstract string PriceSelector { get; }

        public async Task<List<Product>> ParseProducts()
        {
            var products = new List<Product>();
            var web = new HtmlWeb();
            
            try 
            {
                var doc = await web.LoadFromWebAsync(BaseUrl);
                var productNodes = doc.DocumentNode.SelectNodes(ProductSelector);

                if (productNodes != null)
                {
                    foreach (var node in productNodes)
                    {
                        var name = node.SelectSingleNode(NameSelector)?.InnerText.Trim();
                        var price = node.SelectSingleNode(PriceSelector)?.InnerText.Trim();

                        if (!string.IsNullOrEmpty(name))
                        {
                            // Очищаем цену от HTML-сущностей и лишних символов
                            price = price?
                                .Replace("&nbsp;", " ")
                                .Replace("РУБ", "")
                                .Replace("руб", "")
                                .Replace("₽", "")
                                .Trim();

                            products.Add(new Product
                            {
                                Name = name,
                                Price = price ?? string.Empty,
                                Shop = ShopName,
                                ParseDate = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке страницы {ShopName}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутренняя ошибка");
                }
                return new List<Product>();
            }

            return products;
        }

        public async Task ParseAsync()
        {
            try
            {
                var client = _clientFactory.CreateClient("SafeHttpClient");
                var products = await ParseProducts();
                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при парсинге {ShopName}");
                throw;
            }
        }
    }
} 