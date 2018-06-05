using System;

namespace Preprocessor
{
    [Serializable]
    public class ClientMapping
    {
        public string Version { get; set; }

        public TypeMapping[] Types { get; set; }
    }

    [Serializable]
    public class TypeMapping
    {
        public string RefactoredName { get; set; }
        public string ObfuscatedName { get; set; }
        public string BaseType { get; set; }
        public bool IsClass { get; set; }
        public bool IsEnum { get; set; }
        public bool IsInterface { get; set; }
        public bool IsValueType { get; set; }
        public bool IsPublic { get; set; }

        public GenericParameterMapping[] GenericParameters { get; set; }
        public MethodMapping[] Methods { get; set; }
        public PropertyMapping[] Properties { get; set; }
        public EnumValueMapping[] EnumValues { get; set; }
    }

    [Serializable]
    public class EnumValueMapping
    {
        public string RefactoredName { get; set; }
        public string ObfuscatedName { get; set; }
        public int Value { get; set; }
    }

    [Serializable]
    public class PropertyMapping
    {
        public string RefactoredName { get; set; }
        public string ObfuscatedName { get; set; }
        public string Type { get; set; }
        public bool HasPublicGetter { get; set; }
        public bool HasPublicSetter { get; set; }
    }

    [Serializable]
    public class MethodMapping
    {
        public string RefactoredName { get; set; }
        public string ObfuscatedName { get; set; }
        public string ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public GenericParameterMapping[] GenericParameters { get; set; }
        public ParameterMapping[] Parameters { get; set; }
    }

    [Serializable]
    public class GenericParameterMapping
    {
        public string ObfuscatedName { get; set; }
        public string[] Constraints { get; set; }
    }

    [Serializable]
    public class ParameterMapping
    {
        public string Type { get; set; }
        public string ObfuscatedName { get; set; }
    }
}