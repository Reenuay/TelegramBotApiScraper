using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotApiScraper
{
    static internal class Saver
    {
        public static void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        public static void SaveFile(string path, string content)
        {
            File.WriteAllText(
                path,
                $"---\n{content}---\n\n"
                + "```dataviewjs\n"
                + "dv.view('');\n"
                + "```\n"
            );
        }
    }
}
