using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramApiScraper
{
    enum ApiTypeKind
    {
        Record,
        Union,
        Method
    }

    class ApiField
    {
        public int Order { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Desc { get; set; }
    }

    class ApiType
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public List<string> Desc { get; set; } = new List<string>();
        public ApiTypeKind? Kind { get; set; }
        public Dictionary<string, ApiField> Fields { get; set; }
    }

    class Data
    {
        public List<ApiType> Types { get; set; }
            = new List<ApiType>();

        public List<ApiType> Methods { get; set; }
            = new List<ApiType>();
    }

    class Program
    {
        static void Main()
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
                var docText = HtmlEntity.DeEntitize(doc.InnerText);

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
                                    data.Methods.Add(type);
                                }
                                else
                                {
                                    data.Types.Add(type);
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

                                    string fieldName = null;
                                    string fieldType = null;
                                    string fieldDesc = null;
                                    bool fieldRequired = false;

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
                                        );
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
                        Name = typeName,
                        Order = typeOrder,
                        Kind = char.IsUpper(docText.First())
                            ? null
                            : ApiTypeKind.Method
                    };
                    typeOrder++;
                }
            }

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(data, options);

            var filePath = "C:/Users/USER/Desktop/api.raw.json";

            File.WriteAllText(filePath, json);

            Console.WriteLine($"Api types overall: {data.Types.Count}");
            Console.WriteLine($"Api methods overall: {data.Methods.Count}");

            Console.ReadLine();
        }
    }
}
