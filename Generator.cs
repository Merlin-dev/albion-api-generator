using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Preprocessor
{
    public static class Generator
    {
        //Key: Refactored name, Value: Obfuscated name
        private static Dictionary<string, string> typeDictionary = new Dictionary<string, string>();

        //Key: Obfuscated name, Value: Refactored name
        private static Dictionary<string, string> obfTypeDictionary = new Dictionary<string, string>();

        //Key: Refactored name, Value: Obfuscated name
        private static Dictionary<string, string> enumDictionary = new Dictionary<string, string>();

        //Key: Obfuscated name, Value: Refactored name
        private static Dictionary<string, string> obfEnumDictionary = new Dictionary<string, string>();

        private static string template;
        private static string ut_template;
        private static string enum_template;

        private static ClientMapping client_map;

        private static List<string> MethodReflectionPool = new List<string>();
        private static List<string> PropertyReflectionPool = new List<string>();

        public static void AddAdditionalMapping(ClientMapping mapping)
        {
            if(client_map.Version == mapping.Version)
            {
                foreach (TypeMapping type in mapping.Types)
                {
                    try
                    {
                        foreach (TypeMapping original in client_map.Types)
                        {
                            if(original.RefactoredName == type.RefactoredName)
                            {
                                List<MethodMapping> methods = new List<MethodMapping>();
                                if(original.Methods != null)
                                    methods.AddRange(original.Methods);
                                if(type.Methods != null)
                                    methods.AddRange(type.Methods);
                                original.Methods = methods.ToArray();

                                List<PropertyMapping> properties = new List<PropertyMapping>();
                                if(original.Properties != null)
                                    properties.AddRange(original.Properties);
                                if(type.Properties != null)
                                    properties.AddRange(type.Properties);
                                original.Properties = properties.ToArray();

                                List<EnumValueMapping> values = new List<EnumValueMapping>();
                                if(original.EnumValues != null)
                                    values.AddRange(original.EnumValues);
                                if(type.EnumValues != null)
                                    values.AddRange(type.EnumValues);
                                original.EnumValues = values.ToArray();
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            SetupFormats();
        }

        /// <summary>
        /// Generates Unit tests for client map
        /// </summary>
        /// <returns>Dictionary containing File names and File content</returns>
        public static Dictionary<string, string> GenerateUnitTests()
        {
            Dictionary<string, string> generated_files = new Dictionary<string, string>();

            foreach (TypeMapping mapping in client_map.Types)
            {
                //Skip enums
                if (mapping.IsEnum)
                    continue;

                string file = String.Copy(ut_template);

                file = file.Replace("AlbionVersion", client_map.Version);

                var tests = GenereateTests(mapping);

                //Skip classes with no tests
                if (tests == "")
                    continue;

                file = file.Replace("TestsPlaceholder", tests);

                file = file.Replace("RefactoredName", mapping.RefactoredName);
                file = file.Replace("ObfuscatedName", mapping.ObfuscatedName);

                generated_files.Add(mapping.RefactoredName + ".Test.cs", file);
            }

            return generated_files;
        }

        private static string GenereateTests(TypeMapping mapping)
        {
            StringBuilder builder = new StringBuilder();

            foreach (MethodMapping method in mapping.Methods)
            {
                if (!method.IsPublic)
                {
                    builder.AppendLine("[Test]");
                    builder.AppendLine(string.Format("public void {0}_ReflectionTest()", method.RefactoredName));
                    builder.AppendLine("{");
                    builder.AppendLine(string.Format("MethodInfo info = typeof({0}{3}).GetMethod(\"{1}\", new Type[]{{{2}}});", mapping.ObfuscatedName, method.ObfuscatedName, method.Parameters.Typeofs(), mapping.GetGenericTypes()));
                    builder.AppendLine(string.Format("Assert.Null(info,\"Method {0}.{1}({2}.{3}) is null\");", mapping.RefactoredName, method.RefactoredName, mapping.ObfuscatedName, method.ObfuscatedName));
                    builder.AppendLine("}");
                    builder.AppendLine();
                }
            }

            foreach (PropertyMapping property in mapping.Properties)
            {
                if (!property.HasPublicGetter && !property.HasPublicSetter)
                {
                    builder.AppendLine("[Test]");
                    builder.AppendLine(string.Format("public void {0}_ReflectionTest()", property.RefactoredName));
                    builder.AppendLine("{");
                    builder.AppendLine(string.Format("PropertyInfo property = typeof({0}).GetProperty(\"{1}\");", mapping.ObfuscatedName, property.ObfuscatedName));
                    builder.AppendLine(string.Format("Assert.Null(property,\"Property {0}.{1}({2}.{3}) is null\");", mapping.RefactoredName, property.RefactoredName, mapping.ObfuscatedName, property.ObfuscatedName));
                    builder.AppendLine("}");
                    builder.AppendLine();
                }
            }

            if (builder.Length == 0)
                return "";

            string raw = builder.ToString();
            return raw.Substring(0, raw.Length - 1);
        }

        internal static string GenerateProjectFile(string template, IEnumerable<string> filenames)
        {
            string file = File.ReadAllText(template);
            StringBuilder builder = new StringBuilder();
            foreach (string filename in filenames)
            {
                builder.AppendLine(string.Format("<Compile Include=\"{0}\" />", filename));
            }

            return file.Replace("IncludePlaceholder", builder.ToString());
        }

        /// <summary>
        /// Sets client map as source for generating
        /// </summary>
        /// <param name="map">Map</param>
        public static void SetClientMap(ClientMapping map)
        {
            client_map = map;

            SetupFormats();
        }

        private static void SetupFormats()
        {
            //Convert types and names to C# compatible
            foreach (TypeMapping type in client_map.Types)
            {
                type.ObfuscatedName = type.ObfuscatedName.ToCSharp();
                type.RefactoredName = type.RefactoredName.ToCSharp();

                foreach (MethodMapping method in type.Methods)
                {
                    method.ObfuscatedName = method.ObfuscatedName.ToCSharp();
                    method.RefactoredName = method.RefactoredName.ToCSharp();
                    method.ReturnType = method.ReturnType.ToCSharp();
                    foreach (ParameterMapping parameter in method.Parameters)
                    {
                        parameter.ObfuscatedName = parameter.ObfuscatedName.ToCSharp();
                        parameter.Type = parameter.Type.ToCSharp();
                    }
                }

                foreach (PropertyMapping property in type.Properties)
                {
                    property.ObfuscatedName = property.ObfuscatedName.ToCSharp();
                    property.RefactoredName = property.RefactoredName.ToCSharp();
                    property.Type = property.Type.ToCSharp();
                }
            }

            typeDictionary.Clear();
            obfTypeDictionary.Clear();

            //Generates dictionaries used for translation
            foreach (TypeMapping type in client_map.Types)
            {
                //Add enums to separate lists, so i can detect if return type is enum later :D
                if (type.IsEnum && !enumDictionary.ContainsKey(type.RefactoredName))
                {
                    enumDictionary.Add(type.RefactoredName, type.ObfuscatedName);
                    obfEnumDictionary.Add(type.ObfuscatedName, type.RefactoredName);
                }
                typeDictionary.Add(type.RefactoredName, type.ObfuscatedName);
                obfTypeDictionary.Add(type.ObfuscatedName, type.RefactoredName);
            }
        }

        /// <summary>
        /// Loads template from file
        /// </summary>
        /// <param name="path">Path to file</param>
        public static void LoadTemplate(string path)
        {
            //TODO: Check if file exists
            template = File.ReadAllText(path);
        }

        /// <summary>
        /// Loads template for Unit tests from file
        /// </summary>
        /// <param name="path">Path to file</param>
        public static void LoadUTTemplate(string path)
        {
            //TODO: Check if file exists
            ut_template = File.ReadAllText(path);
        }

        /// <summary>
        /// Loads template for enums from file
        /// </summary>
        /// <param name="path">Path to file</param>
        public static void LoadEnumTeplate(string path)
        {
            //TODO: Check if file exists
            enum_template = File.ReadAllText(path);
        }

        /// <summary>
        /// Generates and saves to specified directory
        /// </summary>
        /// <param name="path">Directory</param>
        public static Dictionary<string, string> Generate()
        {
            Dictionary<string, string> generated_files = new Dictionary<string, string>();

            foreach (TypeMapping mapping in client_map.Types)
            {
                string file = "";


                //Skip enums
                if (mapping.IsEnum)
                {
                    file = String.Copy(enum_template);
                    file = file.Replace("ValuesPlaceholder", GenerateEnum(mapping));
                }
                else
                {
                    file = String.Copy(template);

                    file = file.Replace("PropertiesPlaceholder", GenerateProperties(mapping));
                    file = file.Replace("FieldsPlaceholder", GenerateFields(mapping));
                    file = file.Replace("MethodsPlaceholder", GenerateMethods(mapping));
                    file = file.Replace("ReflectionPoolPlaceholder", GeneratePool());


                }

                file = file.Replace("AlbionVersion", client_map.Version);
                file = file.Replace("GenericClass", mapping.GetGenericClass());
                file = file.Replace("GenericWhere", mapping.GetGenericWhere());
                file = file.Replace("GenericName", mapping.GetGenericNames());
                file = file.Replace("BaseHolder", mapping.GetBaseHolder(obfTypeDictionary));
                file = file.Replace("GenericBaseType", mapping.GetGenericBaseTypes(obfTypeDictionary));
                file = file.Replace("RefactoredName", mapping.RefactoredName);
                file = file.Replace("ObfuscatedName", mapping.ObfuscatedName + mapping.GetGenericNames());

                generated_files.Add(mapping.RefactoredName + ".cs", file);
            }

            return generated_files;
        }

        private static string GenerateEnum(TypeMapping mapping)
        {
            if (mapping.EnumValues.Length == 0)
                throw new Exception("Can't be empty");

            StringBuilder builder = new StringBuilder();

            foreach (EnumValueMapping val in mapping.EnumValues)
            {
                builder.AppendFormat("{0} = {1},\n", val.RefactoredName, val.Value);
            }

            return builder.ToString().Substring(0,builder.Length - 2);
        }

        private static string GeneratePool()
        {
            if (MethodReflectionPool.Count == 0 && PropertyReflectionPool.Count == 0)
                return "";

            StringBuilder builder = new StringBuilder();

            foreach (string item in MethodReflectionPool)
            {
                builder.AppendFormat("_methodReflectionPool.Add({0});\n", item);
            }

            foreach (string item in PropertyReflectionPool)
            {
                builder.AppendFormat("_propertyReflectionPool.Add({0});\n", item);
            }

            string raw = builder.ToString();

            MethodReflectionPool.Clear();
            PropertyReflectionPool.Clear();
            return raw.Substring(0, raw.Length - 1);
        }

        private static string GenerateMethods(TypeMapping mapping)
        {
            StringBuilder builder = new StringBuilder();

            foreach (MethodMapping method in mapping.Methods)
            {
                if (method.IsPublic)
                {
                    if (method.GenericParameters.Length > 0)
                    {
                        //TODO: Generate multiple methods based on constrains count
                        builder.AppendFormat("public {0} {1} {2}<{8}>({3}) {7} => ({1}){4}.{5}<{8}>({6}){9};\n",
                            method.IsStatic ? "static" : "",
                            method.ReturnType.TryGetWrappedType(obfTypeDictionary),
                            method.RefactoredName,
                            method.Parameters.TypesAndNames(obfTypeDictionary).Trim(),
                            method.IsStatic ? mapping.ObfuscatedName : "_internal",
                            method.ObfuscatedName,
                            method.Parameters.TypedNames().Trim(),
                            method.GenericParameters.GenerateWhere(),
                            method.GenericParameters.GetGenericNames(),
                            method.ReturnType.IsEnum(obfEnumDictionary) ? ".ToWrapped()" : ""
                            );
                    }
                    else if (method.ReturnType.IsEnumerable() || method.ReturnType.IsList() || method.ReturnType.IsCollection())
                    {
                        builder.AppendFormat("public {0} {1} {2}({3}) => {4}.{5}({6}).Select(x =>({7})x){8};\n",
                            method.IsStatic ? "static" : "",
                            method.ReturnType.TryGetWrappedType(obfTypeDictionary),
                            method.RefactoredName,
                            method.Parameters.TypesAndNames(obfTypeDictionary).Trim(),
                            method.IsStatic ? mapping.ObfuscatedName : "_internal",
                            method.ObfuscatedName,
                            method.Parameters.TypedNames().Trim(),
                            method.ReturnType.TryGetListConvertType(obfTypeDictionary),
                            method.ReturnType.IsList() || method.ReturnType.IsCollection() ? ".ToList()" : ""
                            );
                    }
                    else if (method.ReturnType.IsArray())
                    {
                        builder.AppendFormat("public {0} {1} {2}({3}) => {4}.{5}({6}).Select(x =>({7})x){8};\n",
                            method.IsStatic ? "static" : "",
                            method.ReturnType.TryGetWrappedType(obfTypeDictionary),
                            method.RefactoredName,
                            method.Parameters.TypesAndNames(obfTypeDictionary).Trim(),
                            method.IsStatic ? mapping.ObfuscatedName : "_internal",
                            method.ObfuscatedName,
                            method.Parameters.TypedNames().Trim(),
                            method.ReturnType.TryGetListConvertType(obfTypeDictionary),
                            method.ReturnType.IsArray() ? ".ToArray()" : ""
                            );
                    }
                    else
                    {
                        builder.AppendFormat("public {0} {1} {2}({3}) => {4}.{5}({6}){7};\n",
                            method.IsStatic ? "static" : "",
                            method.ReturnType.TryGetWrappedType(obfTypeDictionary),
                            method.RefactoredName,
                            method.Parameters.TypesAndNames(obfTypeDictionary).Trim(),
                            method.IsStatic ? mapping.ObfuscatedName : "_internal",
                            method.ObfuscatedName,
                            method.Parameters.TypedNames().Trim(),
                            method.ReturnType.IsEnum(obfEnumDictionary) ? ".ToWrapped()" : ""
                            );
                    }
                }
                else
                {
                    //TODO: Finish this so reflection takes note for out and refs (when searching for method)

                    int index = MethodReflectionPool.Count;
                    MethodReflectionPool.Add(string.Format("typeof({0}{3}).GetMethod(\"{1}\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, new Type[]{{{2}}}, null)", mapping.ObfuscatedName, method.ObfuscatedName, method.Parameters.Typeofs(), mapping.GetGenericNames()));

                    string wrappedType = method.ReturnType.TryGetWrappedType(obfTypeDictionary);

                    builder.AppendFormat("public {0} {1} {2}({3}) => {7}_methodReflectionPool[{4}].Invoke({5},new object[]{{{6}}});\n",
                        method.IsStatic ? "static" : "",
                        wrappedType,
                        method.RefactoredName,
                        method.Parameters.TypesAndNames(obfTypeDictionary).Trim(),
                        index,
                        method.IsStatic ? "this" : "_internal",
                        method.Parameters.TypedNames().Trim(),
                        wrappedType == "void" ? "" : "(" + wrappedType + ")"
                        );
                }
            }

            return builder.ToString();
        }

        private static string GenerateFields(TypeMapping mapping)
        {
            //Not implemented
            return "";
        }

        private static string GenerateProperties(TypeMapping mapping)
        {
            StringBuilder builder = new StringBuilder();
            foreach (PropertyMapping prop in mapping.Properties)
            {
                //Regular property
                if (prop.HasPublicGetter && prop.HasPublicSetter)
                {
                    builder.AppendFormat("public {0} {1} \n{{\nget => _internal.{2};\nset => _internal.{2} = value;\n}}\n", prop.Type.TryGetWrappedType(obfTypeDictionary), prop.RefactoredName, prop.ObfuscatedName);
                }
                //Read only property
                else if (prop.HasPublicGetter && !prop.HasPublicSetter)
                {
                    builder.AppendFormat("public {0} {1} => _internal.{2};\n", prop.Type.TryGetWrappedType(obfTypeDictionary), prop.RefactoredName, prop.ObfuscatedName);
                }
                //Reflected property
                else
                {
                    //Register getter and setter to ReflectionPool
                    int getIndex = PropertyReflectionPool.Count;
                    PropertyReflectionPool.Add(string.Format("typeof({0}).GetProperty(\"{1}\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)", mapping.ObfuscatedName, prop.ObfuscatedName));

                    builder.AppendFormat("public {0} {1} \n{{\nget => ({3})_propertyReflectionPool[{2}].GetValue(_internal, null);\nset => _propertyReflectionPool[{2}].SetValue(_internal, ({3})value, null);\n}}\n",
                        prop.Type.TryGetWrappedType(obfTypeDictionary),
                        prop.RefactoredName,
                        getIndex,
                        prop.Type);
                }
            }

            return builder.ToString();
        }
    }
}