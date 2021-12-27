using System.Collections.Generic;

namespace TelegramApiScraper
{
    internal class ApiType
    {
        public int Order { get; set; }
        public List<string> Desc { get; set; } = new List<string>();
        public ApiTypeKind? Kind { get; set; }
        public Dictionary<string, ApiField> Fields { get; set; }
    }
}
