using System.IO;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public sealed class UIBindingNamingContext
    {
        public UIBindingNamingContext(
            string rootGameObjectName,
            string prefabAssetPath,
            string prefabDirectory)
        {
            RootGameObjectName = rootGameObjectName ?? string.Empty;
            PrefabAssetPath = prefabAssetPath ?? string.Empty;
            PrefabDirectory = prefabDirectory ?? string.Empty;
        }

        public UIBindingCollector Collector { get; private set; }
        public Transform RootTransform { get; private set; }
        public string RootGameObjectName { get; private set; }
        public string PrefabAssetPath { get; private set; }
        public string PrefabDirectory { get; private set; }

        public static UIBindingNamingContext FromCollector(UIBindingCollector collector)
        {
            var rootTransform = collector != null ? collector.transform : null;
            var rootName = rootTransform != null ? rootTransform.gameObject.name : string.Empty;
            var prefabAssetPath = GetPrefabAssetPath(collector);
            var prefabDirectory = string.IsNullOrEmpty(prefabAssetPath)
                ? string.Empty
                : NormalizeAssetPath(Path.GetDirectoryName(prefabAssetPath));

            var context = new UIBindingNamingContext(
                rootName,
                prefabAssetPath,
                prefabDirectory);

            context.Collector = collector;
            context.RootTransform = rootTransform;
            return context;
        }

        private static string GetPrefabAssetPath(UIBindingCollector collector)
        {
            if (collector == null)
            {
                return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(collector.gameObject);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return NormalizeAssetPath(assetPath);
            }

            assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collector.gameObject);
            return NormalizeAssetPath(assetPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
