using System;

namespace TelegramApiScraper
{
    internal class Program
    {
        static private void SaveJson(Data data)
        {
            Console.WriteLine("Enter filename...");

            var path = Console.ReadLine();

            Serializer.SerializeTo(path, data);

            Console.WriteLine("Done!");
        }

        static private void Main()
        {
            Console.WriteLine("Getting data from Telegram servers...");

            var data = Scraper.Scrape();

            Console.WriteLine("Data scraped successfuly!");
            Console.WriteLine();
            Console.WriteLine("Choose what you want to do with it:");
            Console.WriteLine("1 to save it to file");

            var showChoice = true;

            while (showChoice)
            {
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        SaveJson(data);
                        showChoice = false;
                        break;

                    default:
                        Console.WriteLine("Wrong choice, choose from list below:");
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
