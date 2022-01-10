using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TelegramBotApiScraper
{
    static internal class Vault
    {
        static private readonly ISerializer serializer =
            new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

        public static void Create(string vaultPath, List<ApiObject> objects)
        {
            (
                var primitives,
                var stubs,
                var records,
                var unions,
                var methods
            )
            = ParseApi(objects);

            (
                var primitivesDir,
                var stubsDir,
                var recordsDir,
                var unionsDir,
                var methodsDir
            )
            = ConstructsPaths(vaultPath);

            Saver.CleanDirectory(primitivesDir);
            Saver.CleanDirectory(stubsDir);
            Saver.CleanDirectory(recordsDir);
            Saver.CleanDirectory(unionsDir);
            Saver.CleanDirectory(methodsDir);

            primitives.SaveToDirectory(primitivesDir);
            stubs.SaveToDirectory(stubsDir);
            records.SaveToDirectory(recordsDir);
            unions.SaveToDirectory(unionsDir);
            methods.SaveToDirectory(methodsDir);
        }

        public static void Update(string vaultPath, List<ApiObject> objects)
        {
            (
                var primitives,
                var stubs,
                var records,
                var unions,
                var methods
            )
            = ParseApi(objects);

            (
                var primitivesDir,
                var stubsDir,
                var recordsDir,
                var unionsDir,
                var methodsDir
            )
            = ConstructsPaths(vaultPath);

            primitives.SaveToDirectory(primitivesDir, true);
            stubs.SaveToDirectory(stubsDir, true);
            records.SaveToDirectory(recordsDir, true);
            unions.SaveToDirectory(unionsDir, true);
            methods.SaveToDirectory(methodsDir, true);
        }

        static private (
            Dictionary<string, object>,
            Dictionary<string, object>,
            Dictionary<string, object>,
            Dictionary<string, object>,
            Dictionary<string, object>
        ) ParseApi(
            List<ApiObject> objects
        )
        {
            (var types, var methodNames, var primitiveNames) =
                GetObjectInfo(objects);

            var typeNames = types.Keys.ToHashSet();

            var primitives = new Dictionary<string, object>();
            var stubs = new Dictionary<string, object>();
            var records = new Dictionary<string, object>();
            var unions = new Dictionary<string, object>();
            var methods = new Dictionary<string, object>();

            var order = 1;

            foreach (var obj in objects)
            {
                var name = obj.Name.ToPascalCase();

                var description = obj.Description
                    .CleanDescription()
                    .CanonicalizeDescription(methodNames);

                if (char.IsUpper(obj.Name[0]))
                {
                    if (obj.Properties.Any())
                    {
                        if (obj.Properties[0].Type != "")
                        {
                            var fields = new List<object>();

                            foreach (var field in obj.Properties)
                            {
                                var fieldName = field.Name.ToPascalCase();

                                var fieldType = field.Type
                                    .ConstructTypeDef(field.Description[0]);

                                var fieldDescription = field.Description
                                    .CleanDescription()
                                    .CanonicalizeDescription(methodNames);

                                fields.Add(new {
                                    name = fieldName,
                                    type = fieldType,
                                    description = fieldDescription
                                });
                            }

                            records.Add(name, new {
                                type = "Record",
                                order,
                                name,
                                description,
                                fields
                            });
                        }
                        else
                        {
                            var cases = new List<object>();

                            foreach (var @case in obj.Properties)
                            {
                                var caseDescription = types[@case.Name]
                                    .Description
                                    .CleanDescription()
                                    .CanonicalizeDescription(methodNames);

                                cases.Add(new {
                                    name = @case.Name,
                                    type = @case.Name,
                                    description = caseDescription
                                });
                            }

                            unions.Add(name, new {
                                type = "Union",
                                order,
                                name,
                                description,
                                cases
                            });
                        }
                    }
                    else
                    {
                        stubs.Add(name, new {
                            type = "Stub",
                            order,
                            name,
                            description
                        });
                    }
                }
                else
                {
                    var parameters = new List<object>();

                    foreach (var parameter in obj.Properties)
                    {
                        var parameterName = parameter.Name.ToPascalCase();

                        var parameterType = parameter.Type
                            .ConstructTypeDef(parameter.Description[0]);

                        var parameterDescription = parameter.Description
                            .CleanDescription()
                            .CanonicalizeDescription(methodNames);

                        parameters.Add(new {
                            name = parameterName,
                            type = parameterType,
                            required = parameter.Required,
                            description = parameterDescription
                        });
                    }

                    description = description.CanonicalizeReturnType();

                    var returns = obj.Description
                        .ExtractReturnType(primitiveNames, typeNames);

                    methods.Add(name, new {
                        type = "Method",
                        order,
                        name,
                        description,
                        returns,
                        parameters
                    });
                }

                order++;
            }

            foreach (var primitiveName in primitiveNames)
            {
                primitives.Add(primitiveName, new {
                    type = "Primitive",
                    name = primitiveName
                });
            }

            return (primitives, stubs, records, unions, methods);
        }

        private static (
            string,
            string,
            string,
            string,
            string
        ) ConstructsPaths(string vaultPath)
        {
            vaultPath = Path.Combine(vaultPath, "Api");

            var primitivesDir = Path.Combine(vaultPath, "Types", "Primitives");
            var stubsDir = Path.Combine(vaultPath, "Types", "Stubs");
            var recordsDir = Path.Combine(vaultPath, "Types", "Records");
            var unionsDir = Path.Combine(vaultPath, "Types", "Unions");
            var methodsDir = Path.Combine(vaultPath, "Methods");

            return (
                primitivesDir,
                stubsDir,
                recordsDir,
                unionsDir,
                methodsDir
            );
        }

        static private void SaveToDirectory(
            this Dictionary<string, object> files,
            string directoryPath,
            bool update = false
        )
        {
            foreach (var (name, file) in files)
            {
                var fileName = Path.Combine(directoryPath, $"{name}.md");

                if (!update || File.Exists(fileName))
                {
                    Saver.SaveFile(
                        fileName,
                        serializer.Serialize(file)
                    );
                }
            }
        }

        static private (
            Dictionary<string,
            ApiObject>, HashSet<string>,
            HashSet<string>
        )  GetObjectInfo(List<ApiObject> objects)
        {
            var types = new Dictionary<string, ApiObject>();
            var methodNames = new HashSet<string>();
            var primitives = new HashSet<string>
            {
                "list",
                "option"
            };

            foreach (var obj in objects)
            {
                if (char.IsUpper(obj.Name[0]))
                {
                    types.Add(obj.Name, obj);
                }
                else
                {
                    methodNames.Add(obj.Name);
                }
            }

            foreach (var obj in objects)
            {
                foreach (var property in obj.Properties)
                {
                    var propertyTypes = property
                        .Type
                        .CleanType(property.Description.FirstOrDefault() ?? "");

                    foreach (
                        var primitive
                        in propertyTypes.Where(t => !types.ContainsKey(t))
                    )
                    {
                        primitives.Add(primitive);
                    }
                }
            }

            return (types, methodNames, primitives);
        }
    }
}
