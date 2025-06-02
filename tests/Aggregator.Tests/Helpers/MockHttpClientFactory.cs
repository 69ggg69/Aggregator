using Moq;
using Moq.Protected;
using System.Net;

namespace Aggregator.Tests.Helpers;

/// <summary>
/// Фабрика для создания мок HTTP клиентов
/// Позволяет симулировать различные HTTP ответы для тестирования парсеров
/// </summary>
public class MockHttpClientFactory
{
    /// <summary>
    /// Создает HTTP клиент, который возвращает указанный HTML контент
    /// </summary>
    /// <param name="htmlContent">HTML контент для возврата</param>
    /// <param name="statusCode">HTTP статус код (по умолчанию 200)</param>
    /// <returns>Настроенный HttpClient</returns>
    public static HttpClient CreateWithResponse(string htmlContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(htmlContent, System.Text.Encoding.UTF8, "text/html")
            });

        return new HttpClient(handlerMock.Object);
    }

    /// <summary>
    /// Создает HTTP клиент, который бросает исключение
    /// </summary>
    /// <param name="exception">Исключение для броска</param>
    /// <returns>Настроенный HttpClient</returns>
    public static HttpClient CreateWithException(Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        return new HttpClient(handlerMock.Object);
    }

    /// <summary>
    /// Создает мок IHttpClientFactory, возвращающий указанный HttpClient
    /// </summary>
    /// <param name="httpClient">HttpClient для возврата</param>
    /// <returns>Мок IHttpClientFactory</returns>
    public static Mock<IHttpClientFactory> CreateFactoryMock(HttpClient httpClient)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        return factoryMock;
    }

    /// <summary>
    /// Создает мок IHttpClientFactory с указанным HTML ответом
    /// </summary>
    /// <param name="htmlContent">HTML контент для возврата</param>
    /// <param name="statusCode">HTTP статус код</param>
    /// <returns>Мок IHttpClientFactory</returns>
    public static Mock<IHttpClientFactory> CreateFactoryMockWithResponse(string htmlContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var httpClient = CreateWithResponse(htmlContent, statusCode);
        return CreateFactoryMock(httpClient);
    }

    /// <summary>
    /// Создает мок IHttpClientFactory, который бросает исключение
    /// </summary>
    /// <param name="exception">Исключение для броска</param>
    /// <returns>Мок IHttpClientFactory</returns>
    public static Mock<IHttpClientFactory> CreateFactoryMockWithException(Exception exception)
    {
        var httpClient = CreateWithException(exception);
        return CreateFactoryMock(httpClient);
    }
} 