using System.IO;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public sealed class MUIConfig : ScriptableObject
    {
        public const string DefaultAssetSavePath = "Assets/Editor/MUFramework/";
        public const string DefaultAssetPath = DefaultAssetSavePath + "MUIConfig.asset";
        public const string DefaultGeneratedOutputPath = "Assets/Scripts/UI/Generated/";

        [SerializeField] private string _assetSavePath = DefaultAssetSavePath;
        [SerializeField] private string _generatedOutputPath = DefaultGeneratedOutputPath;

        public string AssetSavePath
        {
            get => NormalizeAssetPath(_assetSavePath, DefaultAssetSavePath);
            set => _assetSavePath = NormalizeAssetPath(value, DefaultAssetSavePath);
        }

        public string GeneratedOutputPath
        {
            get => NormalizeAssetPath(_generatedOutputPath, DefaultGeneratedOutputPath);
            set => _generatedOutputPath = NormalizeAssetPath(value, DefaultGeneratedOutputPath);
        }

        public static MUIConfig GetOrCreate()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            if (config != null) return config;

            config = CreateInstance<MUIConfig>();
            config.AssetSavePath = DefaultAssetSavePath;
            config.GeneratedOutputPath = DefaultGeneratedOutputPath;

            var directory = Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Directory.CreateDirectory(directory);
            }
            AssetDatabase.CreateAsset(config, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static string GetGeneratedOutputPath()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            if (config == null) return DefaultGeneratedOutputPath;
            return config.GeneratedOutputPath;
        }

        public static string GetAssetSavePath()
        {
            var config = AssetDatabase.LoadAssetAtPath<MUIConfig>(DefaultAssetPath);
            if (config == null) return DefaultAssetSavePath;
            return config.AssetSavePath;
        }

        public static string NormalizeAssetPath(string path)
        {
            return NormalizeAssetPath(path, DefaultGeneratedOutputPath);
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
