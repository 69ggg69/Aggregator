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
    public class ParserManager(IEnumerable<IParser> parsers, ApplicationDbContext context, ILogger<ParserManager> logger)
    {
        private readonly IEnumerable<IParser> _parsers = parsers;
        private readonly ApplicationDbContext _context = context;

        private readonly ILogger<ParserManager> _logger = logger;

        public async Task ParseAllSites()
        {
            foreach (var parser in _parsers)
            {
                try
                {
                    await parser.ParseAsync();
                    
                    var todayProducts = await _context.Products
                        .Where(p => p.Shop == parser.ShopName && 
                               p.ParseDate.Date == DateTime.UtcNow.Date)
                        .ToListAsync();

                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при парсинге {ShopName}", parser.ShopName);
                }
            }
            _logger.LogInformation("Парсинг завершен!");
        }
    }
} 