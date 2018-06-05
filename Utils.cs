using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Preprocessor
{
    public static class Utils
    {
        public static string ToCSharp(this string source)
        {
            if (source == "as")
                source = "@as";

            return source
                //Fix compatibility with C#
                .Replace('/', '.')
                .Replace("`1", "")
                .Replace("`2", "")
                //.Replace("&", "")
                //Replace types to standard format
                .Replace("System.Void", "void")
                .Replace("System.String", "string")
                .Replace("System.Int16", "short")
                .Replace("System.Int32", "int")
                .Replace("System.Int64", "long")
                .Replace("System.Guid", "Guid")
                .Replace("System.Collections.Generic.List", "List")
                .Replace("System.Collections.Generic.Dictionary", "Dictionary")
                .Replace("System.Collections.Generic.ICollection", "ICollection")
                .Replace("System.Collections.Generic.IEnumerable", "IEnumerable")
                .Replace("System.Byte", "byte")
                .Replace("System.Single", "float")
                .Replace("System.Boolean", "bool")
                .Replace("System.Collections.IEnumerable", "IEnumerable")
                .Replace("Albion.Common.Time.GameTimeStamp", "GameTimeStamp")
                .Replace("UnityEngine.RaycastHit", "RaycastHit");
        }

        public static bool IsEnum(this string type, Dictionary<string, string> dict)
        {
            return dict.ContainsKey(type);
        }

        public static void Empty(string directory) => Empty(new System.IO.DirectoryInfo(directory));

        public static void Empty(this System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        public static string TypesAndNames(this ParameterMapping[] parameters, Dictionary<string, string> dict = null)
        {
            if (parameters.Length == 0)
                return "";

            string outString = "";

            foreach (ParameterMapping param in parameters)
            {
                if (dict == null)
                    outString += string.Format("{0} {1} {2}, ", param.Type.GetModifier(), param.Type.RemoveModifiers(), param.ObfuscatedName);
                else
                {
                    outString += string.Format("{0} {1} {2}, ", param.Type.GetModifier(), param.Type.RemoveModifiers().TryGetWrappedType(dict), param.ObfuscatedName);
                }
            }

            return outString.Substring(0, outString.Length - 2);
        }

        public static string GenerateWhere(this GenericParameterMapping[] mapping)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var item in mapping)
            {
                builder.AppendFormat("where {0}:{1} ", item.ObfuscatedName, item.Constraints[0]); //TODO: Add index param, if more constraints is availible
            }
            return builder.ToString();
        }

        public static string GetGenericNames(this GenericParameterMapping[] mapping)
        {
            if (mapping.Length == 0)
                throw new Exception("Can't happen");
            StringBuilder builder = new StringBuilder();
            foreach (var item in mapping)
            {
                builder.AppendFormat("{0}, ", item.ObfuscatedName);
            }

            string raw = builder.ToString();

            return raw.Substring(0, raw.Length - 2);
        }

        public static string GetModifier(this string str)
        {
            if (str.Contains("&"))
            {
                return "out";
            }
            return "";
        }

        public static string RemoveModifiers(this string str)
        {
            return str.Replace("&", "");
        }

        public static string Names(this ParameterMapping[] parameters)
        {
            if (parameters.Length == 0)
                return "";

            string outString = "";

            foreach (ParameterMapping param in parameters)
            {
                outString += string.Format("{0} {1}, ", param.Type.GetModifier(), param.ObfuscatedName);
            }

            return outString.Substring(0, outString.Length - 2);
        }

        public static string TypedNames(this ParameterMapping[] parameters)
        {
            if (parameters.Length == 0)
                return "";

            string outString = "";

            foreach (ParameterMapping param in parameters)
            {
                outString += string.Format("{0} {2}{1}, ", param.Type.GetModifier(), param.ObfuscatedName, param.Type.GetModifier() != "" ? "" : "(" + param.Type + ")");
            }

            return outString.Substring(0, outString.Length - 2);
        }

        public static string Typeofs(this ParameterMapping[] parameters)
        {
            if (parameters.Length == 0)
                return "";

            string outString = "";

            foreach (ParameterMapping param in parameters)
            {
                outString += string.Format("typeof({0}), ", param.Type.RemoveModifiers());
            }

            return outString.Substring(0, outString.Length - 2);
        }

        public static bool IsArray(this string obfuscatedType)
        {
            return obfuscatedType.Contains("[]") && !obfuscatedType.Contains("float");
        }

        public static bool IsList(this string obfuscatedType)
        {
            return obfuscatedType.Contains("List<");
        }

        public static bool IsEnumerable(this string obfuscatedType)
        {
            return obfuscatedType.Contains("IEnumerable<");
        }

        public static bool IsCollection(this string obfuscatedType)
        {
            return obfuscatedType.Contains("ICollection<");
        }



        public static string TryGetWrappedType(this string obfuscatedType, Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(obfuscatedType))
            {
                return dict[obfuscatedType];
            }

            if (obfuscatedType.Contains("[]"))
            {
                string obfListType = obfuscatedType.Substring(0, obfuscatedType.IndexOf("[]"));

                if (dict.ContainsKey(obfListType))
                {
                    return dict[obfListType] + "[]";
                }
            }

            if (obfuscatedType.Contains("<") && obfuscatedType.Contains(">"))
            {
                int ios = obfuscatedType.IndexOf("<") + 1;
                int ioe = obfuscatedType.IndexOf(">");
                string obfListType = obfuscatedType.Substring(ios, ioe - ios);
                string obfContainer = obfuscatedType.Substring(0, ios - 1);

                string returnType = (dict.ContainsKey(obfContainer) ? dict[obfContainer] : obfContainer) + "<";
                
                returnType += (dict.ContainsKey(obfListType) ? dict[obfListType] : obfListType) +">";

                return returnType;
            }

            return obfuscatedType;
        }

        public static string GetGenericClass(this TypeMapping mapping)
        {
            if (mapping.BaseType.Contains("ValueType"))
                return "struct";
            else
                return "class";
        }

        public static string GetGenericWhere(this TypeMapping mapping)
        {
            if (mapping.GenericParameters.Length == 0)
                return "";

            StringBuilder builder = new StringBuilder();

            foreach (var item in mapping.GenericParameters)
            {
                builder.AppendFormat("where {0}:{1} ", item.ObfuscatedName, item.Constraints[0]);
            }

            return builder.ToString().Substring(0, builder.Length - 1);
        }

        public static string GetGenericNames(this TypeMapping mapping)
        {
            if (mapping.GenericParameters.Length == 0)
                return "";

            StringBuilder builder = new StringBuilder("<");

            foreach (var item in mapping.GenericParameters)
            {
                builder.AppendFormat("{0}, ", item.ObfuscatedName);
            }

            return builder.ToString().Substring(0, builder.Length - 2) + ">";
        }

        public static string GetGenericBaseTypes(this TypeMapping mapping, Dictionary<string, string> dict)
        {
            if (mapping.BaseType == null || mapping.BaseType.Contains("System."))
                return "";

            string baseType = mapping.BaseType;

            if (dict.ContainsKey(baseType))
            {
                baseType = dict[baseType];
            } else
            {
                return "";
            }

            StringBuilder builder = new StringBuilder(" : ");

            builder.AppendFormat("{0}", baseType);

            return builder.ToString();
        }

        public static string GetBaseHolder(this TypeMapping mapping, Dictionary<string, string> dict)
        {
            if (mapping.BaseType == null || mapping.BaseType.Contains("System."))
                return "";

            if (dict.ContainsKey(mapping.BaseType))
            {
                return " : base(instance)";
            }
            else
            {
                return "";
            }
        }

        public static string GetGenericTypes(this TypeMapping mapping)
        {
            if (mapping.GenericParameters.Length == 0)
                return "";

            StringBuilder builder = new StringBuilder("<");

            foreach (var item in mapping.GenericParameters)
            {
                builder.AppendFormat("{0}, ", item.Constraints[0]);
            }

            return builder.ToString().Substring(0, builder.Length - 2) + ">";
        }

        public static string TryGetListConvertType(this string obfuscatedType, Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(obfuscatedType))
            {
                return dict[obfuscatedType];
            }

            if (obfuscatedType.Contains("[]"))
            {
                string obfListType = obfuscatedType.Substring(0, obfuscatedType.IndexOf("[]"));

                if (dict.ContainsKey(obfListType))
                {
                    return dict[obfListType];
                }
            }

            if (obfuscatedType.Contains("<") && obfuscatedType.Contains(">"))
            {
                int ios = obfuscatedType.IndexOf("<") + 1;
                int ioe = obfuscatedType.IndexOf(">");
                string obfSubstring = obfuscatedType.Substring(ios, ioe - ios);
                if (dict.ContainsKey(obfSubstring))
                {
                    return dict[obfSubstring];
                }
            }

            return obfuscatedType;
        }

        public static string[] Lines(this string source)
        {
            return source.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public static string Beautify(string source)
        {
            //TODO: Fix the bug-out when line contains {} and it's not suited for indenting (example ClusterExitDescriptor, ItemDescriptor)

            int indent = 0;
            string[] lines = source.Lines();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                int bracketEndCount = line.Count(x => x == '}');
                int bracketStartCount = line.Count(x => x == '{');

                if (bracketEndCount != bracketStartCount)
                    indent -= bracketEndCount;

                line = line.Trim();
                line = Regex.Replace(line, @"\s+", " ");
                line = Indent(indent) + line;

                lines[i] = line;

                if (bracketEndCount != bracketStartCount)
                    indent += bracketStartCount;
            }

            StringBuilder builder = new StringBuilder();
            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }
            return builder.ToString();
        }

        public static string Indent(int count)
        {
            return "".PadLeft(count * 4); //4 spaces per indent level
        }
    }
}