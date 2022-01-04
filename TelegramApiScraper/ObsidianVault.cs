﻿using System;
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
                "True" => "unit",
                "Boolean" => "bool",
                "String" => "string",
                "Messages" => "Message",
                _ => typeName
            };
        }

        static private IEnumerable<string> SplitTypes(string typeName)
        {
            return typeName.Split(
                new string[] { " or ", ", ", " and " },
                StringSplitOptions.RemoveEmptyEntries
            );
        }

        static private IEnumerable<string> GetBaseTypes(
            string typeName,
            string desc
        )
        {
            typeName = typeName.Trim();

            return typeName.StartsWith("Array of")
                ? GetBaseTypes(typeName[8..], desc)
                : SplitTypes(typeName).Select(t => Canonicalize(t, desc))
                ;
        }

        static private int GetListLevel(string typeName)
        {
            return typeName.StartsWith("Array of ")
                ? 1 + GetListLevel(typeName[8..])
                : 0
                ;
        }

        static private string ConstructTypeDef(
            IEnumerable<string> baseTypes,
            int listLevel,
            bool required
        )
        {
            return string.Join(" or ", baseTypes.Select(t => $"[[{t}]]"))
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

        static private string CleanRecordDesc(string desc)
        {
            if (desc.Contains("64 bit"))
            {
                var start = desc.IndexOf(" This identifier");
                var end = desc.IndexOf(" Returned");

                desc = desc[start..end];
            }

            if (desc.Contains("64-bit"))
            {
                var start = desc.IndexOf(" This number");

                desc = desc[ .. start];
            }

            return desc;
        }

        static private string GenerateTypeLink(
            ApiField field,
            bool isParam = false
        )
        {
            var t = GetBaseTypes(field.Type, field.Desc);
            var l = GetListLevel(field.Type);
            var r = isParam || field.Required;

            return ConstructTypeDef(t, l, r);
        }

        static private string MakeMetadata(int order)
        {
            return $"---\nnumber: {order}\n---\n\n";
        }

        static private MdRawMarkdownSpan MakeSpan(string content)
        {
            return new MdRawMarkdownSpan(content);
        }

        static private IEnumerable<MdBlock> MakeHeader(
            string typeName,
            IEnumerable<string> desc
        )
        {
            return new MdBlock[]
                {
                    new MdHeading(typeName, 3),
                    new MdThematicBreak(),
                    new MdHeading("Description", 5)
                }.Concat(
                    desc.Select(
                        p => new MdParagraph(MakeSpan(p))
                    )
                )
                .Append(new MdThematicBreak());
        }

        static private string MakePrimitive() => new MdDocument().ToString();

        static private string MakeStub(
            string typeName,
            int order,
            IEnumerable<string> desc
        )
        {
            return MakeMetadata(order) +
                new MdDocument(MakeHeader(typeName, desc))
                    .ToString();
        }

        static private string MakeRecord(
            string typeName,
            int order,
            IEnumerable<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> fields
        )
        {
            return MakeMetadata(order) +
                new MdDocument(
                    MakeHeader(typeName, desc)
                    .Append(new MdHeading("Fields", 5))
                    .Append(new MdTable(
                        new MdTableRow("Name", "Type", "Description"),
                        fields.Select(
                            f => new MdTableRow(
                                MakeSpan(MakePascalCase(f.Key)),
                                MakeSpan(GenerateTypeLink(f.Value)),
                                MakeSpan(CleanRecordDesc(f.Value.Desc))
                            )
                        )
                    ))
                ).ToString();
        }

        static private string MakeUnion(
            string typeName,
            int order,
            IEnumerable<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> cases,
            Dictionary<string, ApiType> types
        )
        {
            return MakeMetadata(order) +
                new MdDocument(
                    MakeHeader(typeName, desc)
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
                ).ToString();
        }

        static private string MakeMethod(
            string methodName,
            int order,
            List<string> desc,
            IEnumerable<KeyValuePair<string, ApiField>> parameters,
            Dictionary<string, ApiType> types,
            HashSet<string> primitives
        )
        {
            var doc = new MdDocument(
                MakeHeader(methodName, desc)
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

            return MakeMetadata(order) + doc.ToString();
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
                        var baseTypeNames = GetBaseTypes(field.Type, field.Desc);

                        foreach (var baseTypeName in baseTypeNames)
                        {
                            if (!data.Types.ContainsKey(baseTypeName))
                            {
                                set.Add(baseTypeName);
                            }
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
                        var baseTypeNames = GetBaseTypes(field.Type, field.Desc);

                        foreach (var baseTypeName in baseTypeNames)
                        {
                            if (!data.Types.ContainsKey(baseTypeName))
                            {
                                set.Add(baseTypeName);
                            }
                        }
                    }
                }
            }

            return set;
        }

        static private void CleanDirectory(string path, bool update)
        {
            if (!update && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        static private void SaveFile(string path, string content, bool update)
        {
            if (!update || File.Exists(path))
            {
                File.WriteAllText(path, content);
            }
        }

        static internal void Fill(
            string vaultPath,
            Data data,
            bool update = false
        )
        {
            vaultPath = Path.Combine(vaultPath, "Api");

            var primitivesDir = Path.Combine(vaultPath,"Types", "Primitives");
            var stubsDir = Path.Combine(vaultPath, "Types", "Stubs");
            var recordsDir = Path.Combine(vaultPath, "Types", "Records");
            var unionDir = Path.Combine(vaultPath, "Types", "Union");
            var methodsDir = Path.Combine(vaultPath, "Methods");

            CleanDirectory(primitivesDir, update);
            CleanDirectory(stubsDir, update);
            CleanDirectory(recordsDir, update);
            CleanDirectory(unionDir, update);
            CleanDirectory(methodsDir, update);

            var primitives = CollectPrimitives(data);
            var emptyDoc = MakePrimitive();

            foreach (var primitive in primitives)
            {
                var fileName = Path.Combine(primitivesDir, $"{primitive}.md");

                SaveFile(fileName, emptyDoc, update);
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

                            SaveFile(
                                fileName,
                                MakeStub(typeName, type.Order, type.Desc),
                                update
                            );
                        }
                        break;

                    case ApiTypeKind.Record:
                        {
                            var fileName = Path.Combine(
                                recordsDir,
                                $"{typeName}.md"
                            );

                            SaveFile(
                                fileName,
                                MakeRecord(
                                    typeName,
                                    type.Order,
                                    type.Desc,
                                    type.Fields
                                ),
                                update
                            );
                        }
                        break;

                    case ApiTypeKind.Union:
                        {
                            var fileName = Path.Combine(
                                unionDir,
                                $"{typeName}.md"
                            );

                            SaveFile(
                                fileName,
                                MakeUnion(
                                    typeName,
                                    type.Order,
                                    type.Desc,
                                    type.Fields,
                                    data.Types
                                ),
                                update
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

                SaveFile(
                    fileName,
                    MakeMethod(
                        methodName,
                        method.Order,
                        method.Desc,
                        method.Fields,
                        data.Types,
                        primitives
                    ),
                    update
                );
            }
        }
    }
}
