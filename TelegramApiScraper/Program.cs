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

        static private void FillVault(Data data)
        {
            Console.WriteLine("Enter vault directory path...");

            var path = Console.ReadLine();

            ObsidianVault.Fill(path, data);

            Console.WriteLine("Done!");
        }

        static private void UpdateVault(Data data)
        {
            Console.WriteLine("Enter vault directory path...");

            var path = Console.ReadLine();

            ObsidianVault.Fill(path, data, true);

            Console.WriteLine("Done!");
        }

        static private void Main()
        {
            Console.WriteLine("Getting data from Telegram servers...");

            Data data = Scraper.Scrape();

            Console.WriteLine("Data scraped successfuly!");
            Console.WriteLine();
            Console.WriteLine("Choose what you want to do with it:");

            var showChoice = true;

            while (showChoice)
            {
                Console.WriteLine("1 to save it to file");
                Console.WriteLine("2 to save types to vault");
                Console.WriteLine("3 to update types in vault");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        SaveJson(data);
                        showChoice = false;
                        break;

                    case "2":
                        FillVault(data);
                        showChoice = false;
                        break;

                    case "3":
                        UpdateVault(data);
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
