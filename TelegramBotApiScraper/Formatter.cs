using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TelegramBotApiScraper
{
    static internal class Formatter
    {
        private const StringSplitOptions _splitOptions
            = StringSplitOptions.RemoveEmptyEntries;

        private const StringComparison _comparison =
            StringComparison.OrdinalIgnoreCase;

        public static string ToPascalCase(this string name)
        {
            if (name.Contains('_'))
            {
                return
                    CultureInfo
                    .CurrentCulture
                    .TextInfo
                    .ToTitleCase(name.Replace('_', ' '))
                    .Replace(" ", string.Empty);
            }
            else
            {
                return char.ToUpper(name[0]) + name[1..];
            }
        }

        static private string CanonicalizeType(
            this string typeName,
            string description
        )
        {
            return typeName switch
            {
                "Int" => "int",
                "Integer" =>
                    description.Contains("64-bit")
                    || description.Contains("64 bit")
                    ? "int64"
                    : (description.Contains("Unix") ? "DateTime" : "int"),
                "Float" => "float",
                "Float number" => "float",
                "True" => "unit",
                "Boolean" => "bool",
                "String" => "string",
                "Messages" => "Message",
                _ => typeName
            };
        }

        static private IEnumerable<string> SplitTypeNames(this string typeName)
        {
            return typeName.Split(
                new string[] { " or ", ", ", " and " },
                _splitOptions
            );
        }

        static private string DeArrayify(this string typeName)
        {
            return typeName.StartsWith("Array of ")
                ? DeArrayify(typeName[9..])
                : typeName
                ;
        }

        static private int GetArrayLevel(this string typeName)
        {
            return typeName.StartsWith("Array of ")
                ? 1 + GetArrayLevel(typeName[9..])
                : 0
                ;
        }

        public static IEnumerable<string> CleanType(
            this string typeName,
            string description
        )
        {
            return typeName
                .DeArrayify()
                .SplitTypeNames()
                .Select(t => CanonicalizeType(t, description));
        }

        public static IEnumerable<string> ConstructTypeDef(
            this string typeName,
            string description
        )
        {
            var level = typeName.GetArrayLevel();
            var optional = description.StartsWith("Optional.") ? 1 : 0;

            return Enumerable.Repeat("option", optional)
                .Concat(Enumerable.Repeat("list", level))
                .Concat(typeName.CleanType(description));
        }

        public static IEnumerable<string> CleanDescription(
            this IEnumerable<string> description
        )
        {
            return description.Select(CleanDescription);
        }

        static private string CleanDescription(string description)
        {
            description = description
                .Trim()
                .Replace('<', '{')
                .Replace('>', '}')
                .Replace('\n', ' ')
                .Replace("  ", " ")
                .Replace("\n\n", " ")
                .Replace("an array", "a list")
                .Replace("An array", "A list")
                .Replace("an Array", "a list")
                .Replace("An Array", "A list")
                .Replace("array", "list")
                .Replace("Array", "List")
                .Replace("Optional. ", string.Empty)
                .Replace(", see more on currencies", string.Empty)
                .Replace(
                    " See formatting options for more details.", string.Empty
                )
                .Replace(
                    " See Setting up a bot for more details.", string.Empty
                )
                .Replace(
                    " See Linking your domain to the bot for more details.",
                    string.Empty
                )
                .Replace(
                    " See our self-signed guide for details.", string.Empty
                );

            if (description.Contains("64 bit"))
            {
                var start = description.IndexOf(" This identifier");
                var end = description.IndexOf(" Returned");

                description = description[start..end];
            }

            if (description.Contains("64-bit"))
            {
                var start = description.IndexOf(" This number");

                description = description[..start];
            }

            if (description.Contains(" More about"))
            {
                var start = description.IndexOf(" More about");

                description = description[..start];
            }

            if (description.Contains(" More info"))
            {
                var start = description.IndexOf(" More info");
                var end = description.IndexOf('»');

                description = description[..start] + description[end..];
            }

            if (!description.EndsWith('.') && !description.EndsWith(':'))
            {
                description += '.';
            }

            return description;
        }

        public static IEnumerable<string> CanonicalizeDescription(
            this IEnumerable<string> description,
            HashSet<string> methodNames
        )
        {
            return description
                .Select(d =>
                    string.Join(' ',
                        d.Split(' ', _splitOptions)
                            .Select(w => {
                                if ((w.Contains('_') && !w.Contains('/'))
                                    || methodNames.Any(n => w.StartsWith(n)))
                                {
                                    return w.ToPascalCase();
                                }
                                else
                                {
                                    return w;
                                }
                            })
                    )
                );
        }

        public static IEnumerable<string> ExtractReturnType(
            this IEnumerable<string> description,
            HashSet<string> primitiveNames,
            HashSet<string> typeNames
        )
        {
            var returnSentence =
                description
                    .SelectMany(d =>
                        d.Split('.', _splitOptions)
                    )
                    .Where(s =>
                        s.Contains("is returned", _comparison)
                        || s.Contains("returns", _comparison)
                    )
                    .First();

            var listLevel = returnSentence
                .Contains("array of", _comparison) ? 1 : 0;

            return Enumerable.Repeat("list", listLevel)
                .Concat(
                    returnSentence
                        .Split(' ', _splitOptions)
                        .Select(t => t.CanonicalizeType(""))
                        .Where(t =>
                                typeNames.Contains(t)
                                || primitiveNames.Contains(t))
                );
        }

        public static IEnumerable<string> CanonicalizeReturnType(
            this IEnumerable<string> description
        )
        {
            var returnSentence =
                description
                    .SelectMany(d =>
                        d.Split('.', _splitOptions)
                    )
                    .Where(s =>
                        s.Contains("is returned", _comparison)
                        || s.Contains("returns", _comparison)
                    )
                    .First();

            var canonicalized =
                string.Join(
                    ' ',
                    returnSentence
                        .Split(' ')
                        .Select(t => t.CanonicalizeType(""))
                );

            return description
                .Select(d => d.Replace(returnSentence, canonicalized));
        }
    }
}
