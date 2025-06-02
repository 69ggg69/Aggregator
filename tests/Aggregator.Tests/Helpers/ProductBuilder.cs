using Aggregator.Models;

namespace Aggregator.Tests.Helpers;

/// <summary>
/// Builder для создания тестовых объектов Product
/// Позволяет гибко настраивать параметры товара для тестов
/// </summary>
public class ProductBuilder
{
    private Product _product = new()
    {
        Name = "Default Name",
        Price = "0 РУБ",
        Shop = "Default Shop"
    };

    public ProductBuilder WithName(string name)
    {
        _product.Name = name;
        return this;
    }

    public ProductBuilder WithPrice(string price)
    {
        _product.Price = price;
        return this;
    }

    public ProductBuilder WithShop(string shop)
    {
        _product.Shop = shop;
        return this;
    }

    public ProductBuilder WithParseDate(DateTime date)
    {
        _product.ParseDate = date;
        return this;
    }

    public ProductBuilder WithImageUrl(string url)
    {
        _product.ImageUrl = url;
        return this;
    }

    public ProductBuilder WithLocalImagePath(string path)
    {
        _product.LocalImagePath = path;
        return this;
    }

    public ProductBuilder WithId(int id)
    {
        _product.Id = id;
        return this;
    }

    public Product Build() => _product;

    // Предустановленные конфигурации для разных магазинов
    public static ProductBuilder AskStudioProduct() => new ProductBuilder()
        .WithShop("AskStudio")
        .WithParseDate(DateTime.UtcNow);

    public static ProductBuilder ZnwrProduct() => new ProductBuilder()
        .WithShop("ZNWR")
        .WithParseDate(DateTime.UtcNow);

    // Полностью настроенный тестовый товар с валидными данными
    public static ProductBuilder ValidProduct() => new ProductBuilder()
        .WithName("Тестовый товар")
        .WithPrice("1000 РУБ")
        .WithShop("TestShop")
        .WithParseDate(DateTime.UtcNow)
        .WithImageUrl("https://example.com/image.jpg");

    // Создание списка товаров для тестов пагинации и массовых операций
    public static List<Product> CreateMultipleProducts(int count, string shopName = "TestShop")
    {
        return Enumerable.Range(1, count)
            .Select(i => new ProductBuilder()
                .WithName($"Товар {i}")
                .WithPrice($"{i * 100} РУБ")
                .WithShop(shopName)
                .WithParseDate(DateTime.UtcNow.AddDays(-i))
                .Build())
            .ToList();
    }
} 