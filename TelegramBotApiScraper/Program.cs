using System;
using System.Collections.Generic;

namespace TelegramBotApiScraper
{
    internal class Program
    {
        static private void Main()
        {
            Console.WriteLine("Getting data from Telegram servers...");

            var objects = Scraper.GetTelegramBotApiObjects();

            Console.WriteLine("Data scraped successfuly!");
            Console.WriteLine();
            Console.WriteLine("Choose what you want to do with it:");

            var showChoice = true;

            while (showChoice)
            {
                Console.WriteLine("1 to save types to vault");
                Console.WriteLine("2 to update types in vault");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        CreateVault(objects);
                        showChoice = false;
                        break;

                    case "2":
                        UpdateVault(objects);
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

        static private void CreateVault(List<ApiObject> objects)
        {
            Console.WriteLine("Enter vault directory path...");

            var path = Console.ReadLine();

            Vault.Create(path, objects);

            Console.WriteLine("Done!");
        }

        static private void UpdateVault(List<ApiObject> objects)
        {
            Console.WriteLine("Enter vault directory path...");

            var path = Console.ReadLine();

            Vault.Update(path, objects);

            Console.WriteLine("Done!");
        }
    }
}
