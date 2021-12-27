using System.Collections.Generic;

namespace TelegramApiScraper
{
    internal class Data
    {
        public Dictionary<string, ApiType> Types { get; set; }
            = new Dictionary<string, ApiType>();
        public Dictionary<string, ApiType> Methods { get; set; }
            = new Dictionary<string, ApiType>();
    }
}
