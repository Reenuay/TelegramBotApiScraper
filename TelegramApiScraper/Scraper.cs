using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace TelegramBotApiScraper
{
    static internal class Scraper
    {
        static private string FormatDescription(string text)
        {
            text = text
                .Replace("\n\n", " ")
                .Replace("\n", " ")
                .Replace("  ", " ")
                .Replace('<', '{')
                .Replace('>', '}')
                .Replace("Optional. ", "");

            if (!text.EndsWith('.') && !text.EndsWith(':'))
            {
                text += ".";
            }

            return text;
        }

        static internal List<ApiUnit> Scrape()
        {
            var address = @"https://core.telegram.org/bots/api";

            var web = new HtmlWeb();

            var htmlDoc = web.Load(address);

            var docs = htmlDoc
                .DocumentNode
                .SelectSingleNode("//div[@id='dev_page_content']")
                .ChildNodes
                .Where(d => !d.Name.StartsWith('#'));

            var units = new List<ApiUnit>();

            var unitOrder = 0;
            var unitName = (string)null;
            var unitDesc = new List<string>();
            var unitUnits = new List<ApiUnit>();

            foreach (var doc in docs)
            {
                var docName = doc.Name;
                var docText = HtmlEntity.DeEntitize(doc.InnerText);

                if (unitName is not null)
                {
                    switch (docName)
                    {
                        case "hr":
                        case "h3":
                        case "h4":
                            {
                                units.Add(new ApiUnit
                                {
                                    Order = unitOrder,
                                    Name = unitName,
                                    TypeName = unitName,
                                    Description = unitDesc,
                                    Units = unitUnits
                                });

                                unitOrder++;
                                unitName = null;
                                unitDesc = new List<string>();
                                unitUnits = new List<ApiUnit>();
                            }

                            break;

                        case "p":
                        case "blockquote":
                            {
                                unitDesc.Add(docText);
                            }

                            break;

                        case "table":
                            {
                                var rows = doc
                                    .Element("tbody")
                                    .Elements("tr");

                                var fieldOrder = 0;

                                foreach (var row in rows)
                                {
                                    var tds = row
                                        .Elements("td")
                                        .ToArray();

                                    var fieldName = (string)null;
                                    var fieldType = (string)null;
                                    var fieldDesc = (string)null;

                                    if (tds.Length == 4)
                                    {
                                        var required = HtmlEntity.DeEntitize(
                                            tds[2].InnerText
                                        )
                                            == "Yes"
                                            ? ""
                                            : "Optional. "
                                            ;

                                        fieldName = HtmlEntity.DeEntitize(
                                            tds[0].InnerText
                                        );
                                        fieldType = HtmlEntity.DeEntitize(
                                            tds[1].InnerText
                                        );
                                        fieldDesc = required +
                                            HtmlEntity.DeEntitize(
                                                tds[3].InnerText
                                            );
                                    }
                                    else
                                    {
                                        fieldName = HtmlEntity.DeEntitize(
                                            tds[0].InnerText
                                        );
                                        fieldType = HtmlEntity.DeEntitize(
                                            tds[1].InnerText
                                        );
                                        fieldDesc = HtmlEntity.DeEntitize(
                                            tds[2].InnerText
                                        );
                                    }

                                    var field = new ApiUnit
                                    {
                                        Order = fieldOrder,
                                        Name = fieldName,
                                        TypeName = fieldType,
                                        Description = { fieldDesc }
                                    };

                                    unitUnits.Add(field);

                                    fieldOrder++;
                                }
                            }

                            break;

                        case "ul":
                            {

                                var cases = doc.Elements("li");
                                var caseOrder = 0;

                                foreach (var _case in cases)
                                {
                                    var caseName =
                                        HtmlEntity.DeEntitize(_case.InnerText);

                                    var field = new ApiUnit
                                    {
                                        Order = caseOrder,
                                        Name = caseName
                                    };

                                    unitUnits.Add(field);

                                    caseOrder++;
                                }

                            }

                            break;
                    }
                }

                if (docName is "h4" && !docText.Contains(' '))
                {
                    unitName = docText;
                }
            }

            return units;
        }
    }
}
