using Aggregator.Interfaces;
using Aggregator.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Aggregator.Data;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;

namespace Aggregator.ParserServices
{
    public class AskStudioParser : IParser
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<AskStudioParser> _logger;

        public AskStudioParser(ApplicationDbContext context, IHttpClientFactory clientFactory, ILogger<AskStudioParser> logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public string ShopName => "Ask Studio";

        public async Task<List<Product>> ParseProducts()
        {
            var products = new List<Product>();
            var web = new HtmlWeb();
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                web.PreRequest = (request) =>
                {
                    request.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
                    request.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    return true;
                };

                try 
                {
                    var doc = await web.LoadFromWebAsync("https://askstudio.ru/shop/");
                    
                    var productNodes = doc.DocumentNode
                        .SelectNodes("//div[contains(@class,'catalog-list__item')]");

                    if (productNodes != null)
                    {
                        foreach (var node in productNodes)
                        {
                            var name = node
                                .SelectSingleNode(".//a[contains(@class,'card-product__title')]")
                                ?.InnerText.Trim();

                            var price = node
                                .SelectSingleNode(".//div[contains(@class,'product-price__price-current')]")
                                ?.InnerText.Trim();

                            if (!string.IsNullOrEmpty(name))
                            {
                                products.Add(new Product
                                {
                                    Name = name ?? string.Empty,
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
                    _logger.LogError(ex, "Ошибка при загрузке страницы Ask Studio");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex.InnerException, "Внутренняя ошибка");
                    }
                    return new List<Product>();
                }
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
                _logger.LogError(ex, "Ошибка при парсинге Ask Studio");
                throw;
            }
        }
    }
}