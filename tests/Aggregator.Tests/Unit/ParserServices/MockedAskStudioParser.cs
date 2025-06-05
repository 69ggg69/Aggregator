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
/// <param name="context">Контекст базы данных</param>
/// <param name="clientFactory">Фабрика HTTP клиентов</param>
/// <param name="logger">Логгер</param>
/// <param name="imageService">Сервис изображений</param>
public class MockedAskStudioParser(
    ApplicationDbContext context,
    IHttpClientFactory clientFactory,
    ILogger<AskStudioParser> logger,
    ImageService imageService) : AskStudioParser(context, clientFactory, logger, imageService)
{

    /// <summary>
    /// Переопределенный базовый URL, который указывает на локальный тестовый HTML файл
    /// </summary>
    /// <remarks>
    /// Использует file:// протокол для доступа к сохраненному HTML файлу main_shop_page.html
    /// из тестовой директории TestData/HtmlPages/askstudio/
    /// </remarks>
    protected override string BaseUrl =>
        $"file://{TestDataHelper.GetTestFilePath("HtmlPages/askstudio/main_shop_page.html")}";
}