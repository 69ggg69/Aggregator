using Aggregator.Models;
using Microsoft.EntityFrameworkCore;

namespace Aggregator.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed Shops
            if (!await context.Shops.AnyAsync())
            {
                var shops = new[]
                {
                    new Shop 
                    { 
                        Name = "MyShop", 
                        Url = "https://myshop.com",
                        Description = "Основной магазин для демонстрации архитектуры"
                    }
                };

                await context.Shops.AddRangeAsync(shops);
                await context.SaveChangesAsync();
            }

            // Seed Materials
            if (!await context.Materials.AnyAsync())
            {
                var materials = new[]
                {
                    new Material { Name = "Хлопок" },
                    new Material { Name = "Полиэстер" },
                    new Material { Name = "Шерсть" },
                    new Material { Name = "Пух/Перо" },
                    new Material { Name = "Кожа" },
                    new Material { Name = "Деним" },
                    new Material { Name = "Лён" },
                    new Material { Name = "Кашемир" },
                    new Material { Name = "Вискоза" },
                    new Material { Name = "Акрил" }
                };

                await context.Materials.AddRangeAsync(materials);
            }

            // Seed Colors
            if (!await context.Colors.AnyAsync())
            {
                var colors = new[]
                {
                    new Color { Name = "Чёрный", HexCode = "#000000" },
                    new Color { Name = "Белый", HexCode = "#FFFFFF" },
                    new Color { Name = "Красный", HexCode = "#FF0000" },
                    new Color { Name = "Синий", HexCode = "#0000FF" },
                    new Color { Name = "Зелёный", HexCode = "#008000" },
                    new Color { Name = "Жёлтый", HexCode = "#FFFF00" },
                    new Color { Name = "Розовый", HexCode = "#FFC0CB" },
                    new Color { Name = "Серый", HexCode = "#808080" },
                    new Color { Name = "Коричневый", HexCode = "#A52A2A" },
                    new Color { Name = "Оранжевый", HexCode = "#FFA500" },
                    new Color { Name = "Фиолетовый", HexCode = "#800080" },
                    new Color { Name = "Бежевый", HexCode = "#F5F5DC" },
                    new Color { Name = "Тёмно-синий", HexCode = "#000080" },
                    new Color { Name = "Хаки", HexCode = "#F0E68C" }
                };

                await context.Colors.AddRangeAsync(colors);
            }

            // Seed Sizes
            if (!await context.Sizes.AnyAsync())
            {
                var sizes = new[]
                {
                    new Size { Name = "XS" },
                    new Size { Name = "S" },
                    new Size { Name = "M" },
                    new Size { Name = "L" },
                    new Size { Name = "XL" },
                    new Size { Name = "XXL" },
                    new Size { Name = "XXXL" },
                    new Size { Name = "36" },
                    new Size { Name = "38" },
                    new Size { Name = "40" },
                    new Size { Name = "42" },
                    new Size { Name = "44" },
                    new Size { Name = "46" },
                    new Size { Name = "48" },
                    new Size { Name = "50" },
                    new Size { Name = "OneSize" }
                };

                await context.Sizes.AddRangeAsync(sizes);
            }

            // Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    // Root categories
                    new Category { Name = "Одежда", ParentId = null },
                    new Category { Name = "Обувь", ParentId = null },
                    new Category { Name = "Аксессуары", ParentId = null }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                // Get the root category IDs
                var clothingCategory = await context.Categories.FirstAsync(c => c.Name == "Одежда");
                var shoesCategory = await context.Categories.FirstAsync(c => c.Name == "Обувь");
                var accessoriesCategory = await context.Categories.FirstAsync(c => c.Name == "Аксессуары");

                var subCategories = new[]
                {
                    // Clothing subcategories
                    new Category { Name = "Верхняя одежда", ParentId = clothingCategory.Id },
                    new Category { Name = "Рубашки", ParentId = clothingCategory.Id },
                    new Category { Name = "Брюки", ParentId = clothingCategory.Id },
                    new Category { Name = "Платья", ParentId = clothingCategory.Id },
                    new Category { Name = "Футболки", ParentId = clothingCategory.Id },
                    new Category { Name = "Джинсы", ParentId = clothingCategory.Id },
                    new Category { Name = "Свитера", ParentId = clothingCategory.Id },
                    
                    // Shoes subcategories
                    new Category { Name = "Кроссовки", ParentId = shoesCategory.Id },
                    new Category { Name = "Ботинки", ParentId = shoesCategory.Id },
                    new Category { Name = "Туфли", ParentId = shoesCategory.Id },
                    new Category { Name = "Сандали", ParentId = shoesCategory.Id },
                    
                    // Accessories subcategories  
                    new Category { Name = "Сумки", ParentId = accessoriesCategory.Id },
                    new Category { Name = "Часы", ParentId = accessoriesCategory.Id },
                    new Category { Name = "Очки", ParentId = accessoriesCategory.Id },
                    new Category { Name = "Ремни", ParentId = accessoriesCategory.Id }
                };

                await context.Categories.AddRangeAsync(subCategories);
                await context.SaveChangesAsync();

                // Add deeper subcategories
                var outerwearCategory = await context.Categories.FirstAsync(c => c.Name == "Верхняя одежда");
                var deepSubCategories = new[]
                {
                    new Category { Name = "Пуховики", ParentId = outerwearCategory.Id },
                    new Category { Name = "Куртки", ParentId = outerwearCategory.Id },
                    new Category { Name = "Пальто", ParentId = outerwearCategory.Id },
                    new Category { Name = "Плащи", ParentId = outerwearCategory.Id }
                };

                await context.Categories.AddRangeAsync(deepSubCategories);
            }

            // Seed Tags
            if (!await context.Tags.AnyAsync())
            {
                var tags = new[]
                {
                    new Tag { Name = "Лёгкий" },
                    new Tag { Name = "Водонепроницаемый" },
                    new Tag { Name = "Тёплый" },
                    new Tag { Name = "Дышащий" },
                    new Tag { Name = "Стрейч" },
                    new Tag { Name = "Vintage" },
                    new Tag { Name = "Casual" },
                    new Tag { Name = "Formal" },
                    new Tag { Name = "Sport" },
                    new Tag { Name = "Premium" },
                    new Tag { Name = "Eco-friendly" },
                    new Tag { Name = "Limited Edition" },
                    new Tag { Name = "New Arrival" },
                    new Tag { Name = "Sale" },
                    new Tag { Name = "Bestseller" }
                };

                await context.Tags.AddRangeAsync(tags);
            }

            await context.SaveChangesAsync();
        }
    }
} 