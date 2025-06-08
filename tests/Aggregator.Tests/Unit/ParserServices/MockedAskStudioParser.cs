using Microsoft.Extensions.Logging;
using Aggregator.Data;
using Aggregator.Services;
using Aggregator.ParserServices;
using Aggregator.Tests.Fixtures;

namespace Aggregator.Tests.Unit.ParserServices;

/// <summary>
/// Моковая версия AskStudioParser для тестирования с локальными HTML файлами
/// Переопределяет BaseUrl для использования сохраненных тестовых HTML страниц
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр MockedAskStudioParser
/// </remarks>
/// <param name="clientFactory">Фабрика HTTP клиентов</param>
/// <param name="logger">Логгер</param>
/// <param name="imageService">Сервис изображений</param>
public class MockedAskStudioParser(
    IHttpClientFactory clientFactory,
    ILogger<AskStudioParser> logger,
    ImageService imageService) : AskStudioParser(clientFactory, logger, imageService)
{

    /// <summary>
    /// Переопределенный базовый URL, который указывает на локальный тестовый HTML файл
    /// </summary>
    /// <remarks>
    /// Использует file:// протокол для доступа к сохраненному HTML файлу main_shop_page.html
    /// из тестовой директории TestData/HtmlPages/askstudio/
    /// </remarks>
    protected override string BaseUrl =>
        $"file://{TestDataHelper.GetTestFilePath("HtmlPages/askstudio/shop_page_2.html")}";


    /// <summary>
    /// Возвращает базовый URL для тестирования
    /// </summary>
    /// <returns>Базовый URL</returns>
    public string GetBaseUrl()
    {
        return BaseUrl;
    }

    /// <summary>
    /// Возвращает селектор для товаров
    /// </summary>
    /// <returns>Селектор для товаров</returns>
    public string GetProductSelector()
    {
        return ProductSelector;
    }

    /// <summary>
    /// Возвращает селектор для названия товара
    /// </summary>
    /// <returns>Селектор для названия товара</returns>
    public string GetNameSelector()
    {
        return NameSelector;
    }

    /// <summary>
    /// Возвращает селектор для цены товара
    /// </summary>
    /// <returns>Селектор для цены товара</returns>
    public string GetPriceSelector()
    {
        return PriceSelector;
    }

    /// <summary>
    /// Возвращает селектор для изображения товара
    /// </summary>
    /// <returns>Селектор для изображения товара</returns>
    public string GetImageSelector()
    {
        return ImageSelector;
    }
}