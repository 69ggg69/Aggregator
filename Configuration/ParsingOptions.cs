namespace Aggregator.Configuration
{
    /// <summary>
    /// Настройки парсинга
    /// </summary>
    public class ParsingOptions
    {
        public const string SectionName = "Parsing";

        /// <summary>
        /// Задержка между запросами к одному сайту в миллисекундах
        /// </summary>
        public int DelayBetweenRequestsMs { get; set; } = 1000;

        /// <summary>
        /// Папка для сохранения изображений
        /// </summary>
        public string ImagesFolder { get; set; } = "Images";

        /// <summary>
        /// Включить сохранение изображений
        /// </summary>
        public bool SaveImages { get; set; } = true;

        /// <summary>
        /// Включить детальное логирование парсинга
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Список активных парсеров
        /// </summary>
        public List<string> EnabledParsers { get; set; } = new() { "AskStudio", "ZNWR" };
    }
}