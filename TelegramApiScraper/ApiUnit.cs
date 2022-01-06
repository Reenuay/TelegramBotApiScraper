using System.Collections.Generic;

namespace TelegramBotApiScraper
{
    internal record ApiUnit
    {
        public int Order { get; init; }

        public string Name { get; init; } = "";

        public string TypeName { get; init; } = "";

        public List<string> Description { get; init; } = new();

        public List<ApiUnit> Units { get; init; } = new();
    }
}
