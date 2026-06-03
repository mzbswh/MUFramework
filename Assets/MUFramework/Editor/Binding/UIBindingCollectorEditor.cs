using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    [CustomEditor(typeof(UIBindingCollector))]
    public class UIBindingCollectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var collector = (UIBindingCollector)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Scan & Generate Bindings"))
            {
                ScanAndGenerate(collector);
            }
        }

        private static void ScanAndGenerate(UIBindingCollector collector)
        {
            var rule = UIGlobal.BindingNamingRule as UIBindingNamingRule ?? new DefaultUIBindingNamingRule();
            collector.Entries.Clear();
            ScanChildren(collector.transform, collector.transform, collector, rule);
            EditorUtility.SetDirty(collector);

            var outputDir = MUIConfig.GetGeneratedOutputPath();
            UIBindingCodeGenerator.Generate(collector, outputDir);
        }

        private static void ScanChildren(
            Transform root,
            Transform current,
            UIBindingCollector collector,
            UIBindingNamingRule rule)
        {
            foreach (Transform child in current)
            {
                var match = rule.TryMatch(child.name);
                if (match.HasValue)
                {
                    var (fieldName, componentType) = match.Value;
                    var path = GetRelativePath(root, child);
                    collector.Entries.Add(new UIBindingEntry
                    {
                        FieldName = fieldName,
                        ComponentType = componentType.FullName,
                        ComponentRef = componentType == typeof(GameObject) ? null : child.GetComponent(componentType),
                        Path = path,
                    });
                }
                ScanChildren(root, child, collector, rule);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }
    }
}
