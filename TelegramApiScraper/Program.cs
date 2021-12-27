using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramApiScraper
{
    internal class Program
    {
        private static void Main()
        {
            var data = Scraper.Scrape();

            Serializer.SerializeTo("C:/Users/USER/Desktop/api.raw.json", data);

            Console.WriteLine($"Api types overall: {data.Types.Count}");
            Console.WriteLine($"Api methods overall: {data.Methods.Count}");

            Console.ReadLine();
        }
    }
}
