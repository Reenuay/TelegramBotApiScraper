using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace TelegramBotApiScraper
{
    static internal class Scraper
    {
        static internal List<ApiObject> GetTelegramBotApiObjects()
        {
            var address = @"https://core.telegram.org/bots/api";

            var web = new HtmlWeb();

            var htmlDoc = web.Load(address);

            var docs = htmlDoc
                .DocumentNode
                .SelectSingleNode("//div[@id='dev_page_content']")
                .ChildNodes
                .Where(d => !d.Name.StartsWith('#'));

            var objects = new List<ApiObject>();

            var objectName = (string)null;
            var objectDesc = new List<string>();
            var objectProperties = new List<ApiObject>();

            foreach (var doc in docs)
            {
                var docName = doc.Name;
                var docText = HtmlEntity.DeEntitize(doc.InnerText);

                if (objectName is not null)
                {
                    switch (docName)
                    {
                        case "hr":
                        case "h3":
                        case "h4":
                            {
                                objects.Add(new ApiObject
                                {
                                    Name = objectName,
                                    Type = objectName,
                                    Description = objectDesc,
                                    Properties = objectProperties
                                });

                                objectName = null;
                                objectDesc = new List<string>();
                                objectProperties = new List<ApiObject>();
                            }

                            break;

                        case "p":
                        case "blockquote":
                            {
                                objectDesc.Add(docText);
                            }

                            break;

                        case "table":
                            {
                                var rows = doc
                                    .Element("tbody")
                                    .Elements("tr");

                                foreach (var row in rows)
                                {
                                    var tds = row
                                        .Elements("td")
                                        .ToArray();

                                    var propertyName = (string)null;
                                    var propertyType = (string)null;
                                    var propertyReq = true;
                                    var propertyDesc = (string)null;

                                    if (tds.Length == 3)
                                    {
                                        propertyName = HtmlEntity.DeEntitize(
                                            tds[0].InnerText
                                        );
                                        propertyType = HtmlEntity.DeEntitize(
                                            tds[1].InnerText
                                        );
                                        propertyDesc = HtmlEntity.DeEntitize(
                                            tds[2].InnerText
                                        );
                                    }
                                    else
                                    {
                                        propertyName = HtmlEntity.DeEntitize(
                                            tds[0].InnerText
                                        );
                                        propertyType = HtmlEntity.DeEntitize(
                                            tds[1].InnerText
                                        );
                                        propertyReq = HtmlEntity.DeEntitize(
                                            tds[2].InnerText
                                        ) == "Yes";
                                        propertyDesc = HtmlEntity.DeEntitize(
                                            tds[3].InnerText
                                        );
                                    }

                                    var property = new ApiObject
                                    {
                                        Name = propertyName,
                                        Type = propertyType,
                                        Required = propertyReq,
                                        Description = { propertyDesc }
                                    };

                                    objectProperties.Add(property);
                                }
                            }

                            break;

                        case "ul":
                            {
                                var list = doc.Elements("li");

                                foreach (var el in list)
                                {
                                    var propertyName =
                                        HtmlEntity.DeEntitize(
                                            el.InnerText
                                        );

                                    var property = new ApiObject
                                    {
                                        Name = propertyName
                                    };

                                    objectProperties.Add(property);
                                }

                            }

                            break;
                    }
                }

                if (docName is "h4" && !docText.Contains(' '))
                {
                    objectName = docText;
                }
            }

            return objects;
        }
    }
}
