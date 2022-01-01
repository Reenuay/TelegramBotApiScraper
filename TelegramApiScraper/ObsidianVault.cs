using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Grynwald.MarkdownGenerator;
using System.Globalization;

namespace TelegramApiScraper
{
    static internal class ObsidianVault
    {
        static private string GetBaseType(string typeName, string desc)
        {
            typeName = typeName.Trim();

            return typeName.StartsWith("Array of")
                ? GetBaseType(typeName[8..], desc)
                : Canonicalize(typeName, desc)
                ;
        }

        static private string Canonicalize(string typeName, string desc)
        {
            return typeName switch
            {
                "Int" => "int",
                "Integer" =>
                    desc.Contains("64-bit") || desc.Contains("64 bit")
                    ? "int64"
                    : (desc.Contains("Unix") ? "DateTime" : "int"),
                "Float" => "float",
                "Float number" => "float",
                "True" => "bool",
                "Boolean" => "bool",
                "String" => "string",
                "Messages" => "Message",
                _ => typeName
            };
        }

        static private int GetListLevel(string typeName)
        {
            return typeName.StartsWith("Array of")
                ? 1 + GetListLevel(typeName[8..])
                : 0
                ;
        }

        static private string ConstructTypeDef(
            string baseType,
            int listLevel,
            bool required
        )
        {
            return $"[[{baseType}]]"
                + string.Concat(Enumerable.Repeat(" [[list]]", listLevel))
                + (required ? "" : " [[option]]")
                ;
        }

        static private string MakePascalCase(string fieldName)
        {
            return CultureInfo
                .CurrentCulture
                .TextInfo
                .ToTitleCase(fieldName.Replace("_", " "))
                .Replace(" ", string.Empty);
        }

        static private string GenerateTypeLink(
            ApiField field,
            bool isParam = false
        )
        {
            var t = GetBaseType(field.Type, field.Desc);
            var l = GetListLevel(field.Type);
            var r = isParam || field.Required;

            return ConstructTypeDef(t, l, r);
        }

        static private MdRawMarkdownSpan MakeSpan(string content)
        {
            return new MdRawMarkdownSpan(content);
        }

        static private IEnumerable<MdBlock> MakeHeader(
            string typeName,
            int order,
            IEnumerable<string> desc
        )
        {
            return new MdBlock[]
                {

                    new MdHeading(typeName, 3),
                    new MdThematicBreak(),
                    new MdHeading("Number", 5),
                    new MdParagraph(MakeSpan(order.ToString())),
                    new MdThematicBreak(),
                    new MdHeading("Description", 5)
                }.Concat(
                    desc.Select(
                        p => new MdParagraph(MakeSpan(p))
                    )
                )
                .Append(new MdThematicBreak());
        }

        static private MdDocument MakePrimitive() => new();

        static private MdDocument MakeStub(
            string typeName,
            int order,
            IEnumerable<string> desc
        )
        {
            return new MdDocument(MakeHeader(typeName, order, desc));
        }

        static private MdDocument MakeRecord(
            string typeName,
            int order,
            IEnumerable<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> fields
        )
        {
            return new MdDocument(
                MakeHeader(typeName, order, desc)
                .Append(new MdHeading("Fields", 5))
                .Append(new MdTable(
                    new MdTableRow("Name", "Type", "Description"),
                    fields.Select(
                        f => new MdTableRow(
                            MakeSpan(MakePascalCase(f.Key)),
                            MakeSpan(GenerateTypeLink(f.Value)),
                            MakeSpan(f.Value.Desc)
                        )
                    )
                ))
            );
        }

        static private MdDocument MakeUnion(
            string typeName,
            int order,
            IEnumerable<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> cases,
            Dictionary<string, ApiType> types
        )
        {
            return new MdDocument(
                MakeHeader(typeName, order, desc)
                .Append(new MdHeading("Cases", 5))
                .Append(new MdTable(
                    new MdTableRow("Name", "Type", "Description"),
                    cases.Select(
                        f => new MdTableRow(
                            MakeSpan(f.Key),
                            MakeSpan(GenerateTypeLink(f.Value)),
                            MakeSpan(types[f.Key].Desc[0])
                        )
                    )
                ))
            );
        }

        static private MdDocument MakeMethod(
            string methodName,
            int order,
            List<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> parameters,
            Dictionary<string, ApiType> types,
            HashSet<string> primitives
        )
        {
            var doc = new MdDocument(
                MakeHeader(methodName, order, desc)
            );

            var comparison = StringComparison.OrdinalIgnoreCase;

            var returnSentence = desc
                .SelectMany(d =>
                    d.Split('.', StringSplitOptions.RemoveEmptyEntries)
                )
                .Where(s =>
                    s.Contains("is returned", comparison)
                    || s.Contains("returns", comparison)
                )
                .First()
                .Trim();

            var returnTypes =
                string.Join(
                    " | ",
                    returnSentence
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => Canonicalize(t, ""))
                        .Where(t =>
                            types.ContainsKey(t) || primitives.Contains(t)
                        )
                        .Select(t =>
                            $"[[{t}]]" +
                                (returnSentence
                                    .Contains("array of", comparison)
                                    ? " [[list]]"
                                    : "")
                        )
                );

            doc.Root.Add(new MdHeading("Returns", 5));
            doc.Root.Add(new MdParagraph(MakeSpan(returnTypes)));
            doc.Root.Add(new MdThematicBreak());

            if (parameters is not null) {
                doc.Root.Add(new MdHeading("Parameters", 5));
                doc.Root.Add(new MdTable(
                    new MdTableRow("Name", "Type", "Required", "Description"),
                    parameters.Select(
                        p => new MdTableRow(
                            MakeSpan(MakePascalCase(p.Key)),
                            MakeSpan(GenerateTypeLink(p.Value, true)),
                            MakeSpan(p.Value.Required ? "Yes" : "No"),
                            MakeSpan(p.Value.Desc)
                        )
                    )
                ));
            }

            return doc;
        }

        static private HashSet<string> CollectPrimitives(Data data)
        {
            var set = new HashSet<string>
            {
                "option",
                "list"
            };

            foreach (var (typeName, type) in data.Types)
            {
                if (type.Kind is ApiTypeKind.Record)
                {
                    foreach (var (_, field) in type.Fields)
                    {
                        var baseTypeName = GetBaseType(field.Type, field.Desc);

                        if (!data.Types.ContainsKey(baseTypeName))
                        {
                            set.Add(baseTypeName);
                        }
                    }
                }
            }

            foreach (var (methodNames, method) in data.Methods)
            {
                if (method.Fields is not null)
                {
                    foreach (var (_, field) in method.Fields)
                    {
                        var baseTypeName = GetBaseType(field.Type, field.Desc);

                        if (!data.Types.ContainsKey(baseTypeName))
                        {
                            set.Add(baseTypeName);
                        }
                    }
                }
            }

            return set;
        }

        static internal void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        static internal void Fill(string vaultPath, Data data)
        {
            vaultPath = Path.Combine(vaultPath, "Api");

            var primitivesDir = Path.Combine(vaultPath,"Types", "Primitives");
            var stubsDir = Path.Combine(vaultPath, "Types", "Stubs");
            var recordsDir = Path.Combine(vaultPath, "Types", "Records");
            var unionDir = Path.Combine(vaultPath, "Types", "Union");
            var methodsDir = Path.Combine(vaultPath, "Methods");

            CleanDirectory(primitivesDir);
            CleanDirectory(stubsDir);
            CleanDirectory(recordsDir);
            CleanDirectory(unionDir);
            CleanDirectory(methodsDir);

            var primitives = CollectPrimitives(data);
            var emptyDoc = MakePrimitive().ToString();

            foreach (var primitive in primitives)
            {
                var fileName = Path.Combine(primitivesDir, $"{primitive}.md");

                File.WriteAllText(fileName, emptyDoc);
            }

            foreach (var (typeName, type) in data.Types)
            {
                switch (type.Kind)
                {
                    case null:
                        {
                            var fileName = Path.Combine(
                                stubsDir,
                                $"{typeName}.md"
                            );

                            File.WriteAllText(
                                fileName,
                                MakeStub(typeName, type.Order, type.Desc)
                                .ToString()
                            );
                        }
                        break;

                    case ApiTypeKind.Record:
                        {
                            var fileName = Path.Combine(
                                recordsDir,
                                $"{typeName}.md"
                            );

                            File.WriteAllText(
                                fileName,
                                MakeRecord(
                                    typeName,
                                    type.Order,
                                    type.Desc,
                                    type.Fields
                                )
                                .ToString()
                            );
                        }
                        break;

                    case ApiTypeKind.Union:
                        {
                            var fileName = Path.Combine(
                                unionDir,
                                $"{typeName}.md"
                            );

                            File.WriteAllText(
                                fileName,
                                MakeUnion(
                                    typeName,
                                    type.Order,
                                    type.Desc,
                                    type.Fields,
                                    data.Types
                                )
                                .ToString()
                            );
                        }
                        break;
                }
            }

            foreach (var (tempName, method) in data.Methods)
            {
                var methodName = char.ToUpper(tempName[0]) + tempName[1..];

                var fileName = Path.Combine(
                    methodsDir,
                    $"{methodName}.md"
                );

                File.WriteAllText(
                    fileName,
                    MakeMethod(
                        methodName,
                        method.Order,
                        method.Desc,
                        method.Fields,
                        data.Types,
                        primitives
                    )
                    .ToString()
                );
            }
        }
    }
}
