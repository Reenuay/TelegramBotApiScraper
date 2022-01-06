using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramBotApiScraper
{
    static internal class Serializer
    {
        static internal void SerializeTo(string filePath, List<ApiUnit> units)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(units, options);

            File.WriteAllText(filePath, json);
        }
    }
}
