using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace TelegramApiScraper
{
    static internal class Scraper
    {
        static internal Data Scrape()
        {
            var address = @"https://core.telegram.org/bots/api";

            var web = new HtmlWeb();

            var htmlDoc = web.Load(address);

            var docs = htmlDoc
                .DocumentNode
                .SelectSingleNode("//div[@id='dev_page_content']")
                .ChildNodes
                .Where(d => !d.Name.StartsWith('#'));

            var data = new Data();

            var typeName = (string)null;
            var type = (ApiType)null;
            var typeOrder = 0;

            foreach (var doc in docs)
            {
                var docName = doc.Name;
                var docText = HtmlEntity
                    .DeEntitize(doc.InnerText)
                    .Trim()
                    .Replace("\n\n", " ")
                    .Replace("\n", " ");

                if (type is not null)
                {
                    switch (docName)
                    {
                        case "hr":
                        case "h3":
                        case "h4":
                            {
                                if (type.Kind == ApiTypeKind.Method)
                                {
                                    data.Methods.Add(typeName, type);
                                }
                                else
                                {
                                    data.Types.Add(typeName, type);
                                }

                                typeName = null;
                                type = null;
                            }

                            break;

                        case "p":
                        case "blockquote":
                            {
                                type.Desc.Add(docText);
                            }

                            break;

                        case "table":
                            {
                                var rows = doc
                                    .Element("tbody")
                                    .Elements("tr");

                                var fieldOrder = 0;

                                if (type.Kind is null)
                                    type.Kind = ApiTypeKind.Record;

                                if (type.Fields is null)
                                    type.Fields =
                                        new Dictionary<string, ApiField>();

                                foreach (var row in rows)
                                {
                                    var tds = row
                                        .Elements("td")
                                        .ToArray();

                                    var fieldName = (string)null;
                                    var fieldType = (string)null;
                                    var fieldDesc = (string)null;
                                    var fieldRequired = false;

                                    if (type.Kind == ApiTypeKind.Method)
                                    {
                                        fieldName = HtmlEntity.DeEntitize(
                                            tds[0].InnerText
                                        );
                                        fieldType = HtmlEntity.DeEntitize(
                                            tds[1].InnerText
                                        );
                                        fieldRequired = HtmlEntity.DeEntitize(
                                            tds[2].InnerText
                                        ) == "Yes";
                                        fieldDesc = HtmlEntity.DeEntitize(
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
                                        ).Replace("  ", " ");
                                        fieldRequired = !fieldDesc.StartsWith("Optional.");
                                    }

                                    var field = new ApiField
                                    {
                                        Order = fieldOrder,
                                        Type = fieldType,
                                        Desc = fieldDesc,
                                        Required = fieldRequired
                                    };

                                    type.Fields.Add(fieldName, field);

                                    fieldOrder++;
                                }

                            }

                            break;

                        case "ul":
                            {
                                var cases = doc.Elements("li");

                                var fieldOrder = 0;

                                type.Kind = ApiTypeKind.Union;

                                if (type.Fields is null)
                                    type.Fields =
                                        new Dictionary<string, ApiField>();

                                foreach (var _case in cases)
                                {
                                    var text =
                                        HtmlEntity.DeEntitize(_case.InnerText);

                                    var field = new ApiField
                                    {
                                        Order = fieldOrder
                                    };

                                    type.Fields.Add(text, field);

                                    fieldOrder++;
                                }

                            }

                            break;
                    }
                }

                if (docName is "h4" && !docText.Contains(' '))
                {
                    Console.WriteLine($"{docText} — {typeOrder}");

                    typeName = docText;
                    type = new ApiType
                    {
                        Order = typeOrder,
                        Kind = char.IsUpper(docText.First())
                            ? null
                            : ApiTypeKind.Method
                    };
                    typeOrder++;
                }
            }

            return data;
        }
    }
}
