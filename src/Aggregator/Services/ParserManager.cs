using System;
using System.Net.Http;
using System.Threading.Tasks;
using Aggregator.Data;
using Aggregator.ParserServices;
using HtmlAgilityPack;
using Microsoft.Extensions.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Aggregator.Interfaces;

namespace Aggregator.Services
{
    public class ParserManager
    {
        private readonly IEnumerable<IParser> _parsers;
        private readonly ApplicationDbContext _context;

        public ParserManager(IEnumerable<IParser> parsers, ApplicationDbContext context)
        {
            _parsers = parsers;
            _context = context;
        }

        public async Task ParseAllSites()
        {
            foreach (var parser in _parsers)
            {
                Console.WriteLine($"Начинаем парсинг {parser.ShopName}...");
                try
                {
                    await parser.ParseAsync();
                    
                    var todayProducts = await _context.Products
                        .Where(p => p.Shop == parser.ShopName && 
                               p.ParseDate.Date == DateTime.UtcNow.Date)
                        .ToListAsync();

                    Console.WriteLine($"\nРезультаты парсинга {parser.ShopName}:");
                    Console.WriteLine($"Найдено товаров: {todayProducts.Count}");
                    
                    foreach (var product in todayProducts)
                    {
                        Console.WriteLine($"- {product.Name}: {product.Price}");
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при парсинге {parser.ShopName}: {ex.Message}");
                }
            }
            Console.WriteLine("Парсинг завершен!");
        }
    }
} 