using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotApiScraper
{
    static internal class Formatter
    {
        static internal string UniformDescription(string description)
        {
            description = description
                .Replace('<', '{')
                .Replace('>', '}')
                .Replace("\n", " ")
                .Replace("  ", " ")
                .Replace("\n\n", " ")
                .Replace("Optional. ", "");

            if (!description.EndsWith('.') && !description.EndsWith(':'))
            {
                description += ".";
            }

            return description;
        }
    }
}
