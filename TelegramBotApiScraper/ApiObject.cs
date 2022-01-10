using System.Collections.Generic;

namespace TelegramBotApiScraper
{
    internal record ApiObject
    {
        public string Name { get; init; } = "";

        public string Type { get; init; } = "";

        public bool Required { get; init; }

        public List<string> Description { get; init; } = new();

        public List<ApiObject> Properties { get; init; } = new();
    }
}
