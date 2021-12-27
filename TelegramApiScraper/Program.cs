using System;

namespace TelegramApiScraper
{
    internal class Program
    {
        static private void Main()
        {
            var data = Scraper.Scrape();

            Serializer.SerializeTo("C:/Users/USER/Desktop/api.raw.json", data);

            Console.WriteLine($"Api types overall: {data.Types.Count}");
            Console.WriteLine($"Api methods overall: {data.Methods.Count}");

            Console.ReadLine();
        }
    }
}
