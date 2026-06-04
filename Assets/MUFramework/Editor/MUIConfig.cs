using System.IO;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public sealed class MUIConfig : ScriptableObject
    {
        public const string DefaultAssetSavePath = "Assets/Editor/MUFramework/";
        public const string DefaultAssetPath = DefaultAssetSavePath + "MUIConfig.asset";
        public const string DefaultGeneratedBindOutputPath = "Assets/Scripts/UI/Generated/Bind";
        public const string DefaultGeneratedScriptOutputPath = "Assets/Scripts/UI/";
        public const string DefaultNamespace = "";

        [SerializeField] private string _assetSavePath = DefaultAssetSavePath;
        [SerializeField] private string _generatedBindOutputPath = DefaultGeneratedBindOutputPath;
        [SerializeField] private string _generatedScriptOutputPath = DefaultGeneratedScriptOutputPath;
        [SerializeField] private string _defaultNamespace = DefaultNamespace;

        public string AssetSavePath
        {
            get => NormalizeAssetPath(_assetSavePath, DefaultAssetSavePath);
            set => _assetSavePath = NormalizeAssetPath(value, DefaultAssetSavePath);
        }

        public string GeneratedBindOutputPath
        {
            get => NormalizeAssetPath(_generatedBindOutputPath, DefaultGeneratedBindOutputPath);
            set => _generatedBindOutputPath = NormalizeAssetPath(value, DefaultGeneratedBindOutputPath);
        }

        public string GeneratedScriptOutputPath
        {
            get => NormalizeAssetPath(_generatedScriptOutputPath, DefaultGeneratedScriptOutputPath);
            set => _generatedScriptOutputPath = NormalizeAssetPath(value, DefaultGeneratedScriptOutputPath);
        }

        public string DefaultNamespaceValue
        {
            get => _defaultNamespace ?? DefaultNamespace;
            set => _defaultNamespace = value ?? DefaultNamespace;
        }

        public static MUIConfig GetOrCreate()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            if (config != null) return config;

            config = CreateInstance<MUIConfig>();
            config.AssetSavePath = DefaultAssetSavePath;
            config.GeneratedBindOutputPath = DefaultGeneratedBindOutputPath;
            config.GeneratedScriptOutputPath = DefaultGeneratedScriptOutputPath;
            config.DefaultNamespaceValue = DefaultNamespace;

            var directory = Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Directory.CreateDirectory(directory);
            }
            AssetDatabase.CreateAsset(config, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static string GetGeneratedBindOutputPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            return config == null ? DefaultGeneratedBindOutputPath : config.GeneratedBindOutputPath;
        }

        public static string GetGeneratedScriptOutputPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            return config == null ? DefaultGeneratedScriptOutputPath : config.GeneratedScriptOutputPath;
        }

        public static string GetAssetSavePath()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            return config == null ? DefaultAssetSavePath : config.AssetSavePath;
        }

        public static string GetDefaultNamespace()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            return config == null ? DefaultNamespace : config.DefaultNamespaceValue;
        }

        public static string NormalizeAssetPath(string path)
        {
            return NormalizeAssetPath(path, DefaultGeneratedBindOutputPath);
        }

        public static string NormalizeAssetPath(string path, string defaultPath)
        {
            if (string.IsNullOrWhiteSpace(path)) return defaultPath;
            path = path.Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/") && path != "Assets")
            {
                path = "Assets/" + path.TrimStart('/');
            }
            if (!path.EndsWith("/"))
            {
                path += "/";
            }
            return path;
        }
    }
}
