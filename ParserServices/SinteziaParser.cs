using System;
using Aggregator.Interfaces;
using Aggregator.Models;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Aggregator.Data;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Net;

namespace Aggregator.ParserServices
{
    public class SinteziaParser : IParser
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public SinteziaParser(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        public string ShopName => "Sintezia";

        public async Task<List<Product>> ParseProducts()
        {
            var products = new List<Product>();
            var web = new HtmlWeb();
            
            // Настраиваем игнорирование SSL ошибок
            web.PreRequest = (request) =>
            {
                if (request is HttpWebRequest webRequest)
                {
                    webRequest.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                    webRequest.AutomaticDecompression = DecompressionMethods.All;
                }
                return true;
            };

            try 
            {
                var doc = await web.LoadFromWebAsync("https://sintezia.com/clothing_en");
                
                var productNodes = doc.DocumentNode
                    .SelectNodes("//div[contains(@class,'product-layout')]");

                if (productNodes != null)
                {
                    foreach (var node in productNodes)
                    {
                        var name = node
                            .SelectSingleNode(".//div[contains(@class,'name')]")
                            ?.InnerText.Trim();

                        // Сначала пробуем найти цену со скидкой
                        var newPrice = node
                            .SelectSingleNode(".//span[contains(@class,'price-new')]")
                            ?.InnerText.Trim();

                        // Если цены со скидкой нет, берем обычную цену
                        var price = string.IsNullOrEmpty(newPrice) 
                            ? node.SelectSingleNode(".//div[contains(@class,'price')]")?.InnerText.Trim()
                            : newPrice;

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
                Console.WriteLine($"Ошибка при загрузке страницы Sintezia: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                    Console.WriteLine($"Стек вызовов: {ex.InnerException.StackTrace}");
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
                Console.WriteLine($"Ошибка при парсинге Sintezia: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }
    }
}