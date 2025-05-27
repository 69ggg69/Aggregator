using Aggregator.Data;
using Aggregator.Interfaces;
using Aggregator.Models;
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

                var uniqueProducts = new HashSet<string>();

                if (productNodes != null)
                {
                    foreach (var node in productNodes)
                    {
                        var name = node.SelectSingleNode(NameSelector)?.InnerText.Trim();
                        var price = node.SelectSingleNode(PriceSelector)?.InnerText.Trim();

                        if (!string.IsNullOrEmpty(name))
                        {
                            price = price?
                                .Replace("&nbsp;", " ")
                                .Replace("РУБ", "")
                                .Replace("руб", "")
                                .Replace("₽", "")
                                .Trim();

                            var productKey = $"{name}_{price}";

                            if (!uniqueProducts.Contains(productKey))
                            {
                                uniqueProducts.Add(productKey);
                                products.Add(new Product
                                {
                                    Name = name,
                                    Price = price ?? string.Empty,
                                    Shop = ShopName,
                                    ParseDate = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                _logger.LogInformation($"Пропущен дубликат товара: {name} - {price}");
                            }
                        }
                    }
                }

                _logger.LogInformation($"Найдено {products.Count} уникальных товаров из {productNodes?.Count ?? 0} элементов");
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
                
                var existingProducts = await _context.Products
                    .Where(p => p.Shop == ShopName && p.ParseDate.Date == DateTime.UtcNow.Date)
                    .ToListAsync();
                
                var newProducts = products
                    .Where(p => !existingProducts.Any(ep => 
                        ep.Name == p.Name && 
                        ep.Price == p.Price))
                    .ToList();

                if (newProducts.Any())
                {
                    await _context.Products.AddRangeAsync(newProducts);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Добавлено {newProducts.Count} новых товаров");
                }
                else
                {
                    _logger.LogInformation("Новых товаров не обнаружено");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при парсинге {ShopName}");
                throw;
            }
        }
    }
} 