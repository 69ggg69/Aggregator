using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ClothingStoreScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Замените URL на реальный адрес магазина одежды
            var url = "https://sintezia.com/clothing_en";
            var products = ParseClothingProducts(url);

            // Путь к файлу для сохранения результатов
            string filePath = "parsing_results.txt";

            // Записываем результаты в файл
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Результаты парсинга {DateTime.Now}\n");
                writer.WriteLine($"URL: {url}\n");
                writer.WriteLine("Найденные товары:\n");

                foreach (var product in products)
                {
                    writer.WriteLine($"Название: {product.Name}");
                    writer.WriteLine($"Цена: {product.Price}");
                    writer.WriteLine("------------------------");
                }
            }

            Console.WriteLine($"Результаты сохранены в файл: {Path.GetFullPath(filePath)}");
        }

        static List<ClothingProduct> ParseClothingProducts(string url)
        {
            var products = new List<ClothingProduct>();
            var web = new HtmlWeb();
            var doc = GetDocument(url);

            // 1) контейнер всех карточек — здесь ul с классом catalog_grid, внутри li
            var productNodes = doc.DocumentNode
                .SelectNodes("//div[contains(@class,'product-layout')]");

            if (productNodes != null)
            {
                foreach (var node in productNodes)
                {
                    var product = new ClothingProduct
                    {
                        // 2) название — внутри <a class="catalog_item__name">
                        Name = node
                            .SelectSingleNode(".//div[contains(@class,'name')]")
                            ?.InnerText.Trim(),

                        // 3) цена — внутри <div class="catalog_item__price">
                        Price = node
                            .SelectSingleNode(".//div[contains(@class,'price')]")
                            ?.InnerText.Trim()
                    };

                    if (!string.IsNullOrEmpty(product.Name))
                    {
                        products.Add(product);
                    }
                }
            }

            return products;
        }

        static HtmlDocument GetDocument(string url)
        {
            var web = new HtmlWeb();
            return web.Load(url);
        }
    }

    class ClothingProduct
    {
        public string Name { get; set; }
        public string Price { get; set; }
    }
}
