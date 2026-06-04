using System.IO;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public static class UIBindingCodeGenerator
    {
        public static void Generate(UIAutoGenerator collector, string outputDir)
        {
            if (collector == null || string.IsNullOrEmpty(collector.TargetClassName)) return;
            if (collector.Entries == null || collector.Entries.Count == 0) return;
            outputDir = MUIConfig.NormalizeAssetPath(outputDir);

            var rule = UIGlobal.BindingNamingRule as UIBindingNamingRule ?? new DefaultUIBindingNamingRule();

            var sb = new StringBuilder();
            foreach (var line in rule.ToFileHeaderLines())
                sb.AppendLine(line);
            foreach (var usingLine in rule.ToUsingDirectives())
                sb.AppendLine(usingLine);
            sb.AppendLine();

            bool hasNamespace = !string.IsNullOrEmpty(collector.TargetNamespace);
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {collector.TargetNamespace}");
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";
            var baseClause = ResolveTargetBaseClause(collector);
            sb.AppendLine($"{indent}public partial class {collector.TargetClassName}{baseClause}");
            sb.AppendLine($"{indent}{{");
            foreach (var entry in collector.Entries)
            {
                if (!CanGenerateEntry(entry))
                    continue;
                sb.AppendLine(rule.ToFieldDeclaration(indent + "    ", entry.Type, entry.FieldName));
            }

            sb.AppendLine();
            sb.AppendLine(rule.ToAutoBindMethodDeclaration(indent + "    "));
            sb.AppendLine($"{indent}    {{");
            foreach (var entry in collector.Entries)
            {
                if (!CanGenerateEntry(entry))
                    continue;
                var path = GetRelativePath(collector.transform, entry.GameObject.transform);
                var escapedPath = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.AppendLine(rule.ToBindStatement(indent + "        ", entry.FieldName, entry.Type, escapedPath));
            }
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
                sb.AppendLine("}");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", outputDir));
            var filePath = Path.Combine(outputDir, $"{collector.TargetClassName}.AutoBind.cs");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[MUI] Generated: {filePath}");
        }

        private static bool CanGenerateEntry(UIBindingEntry entry)
        {
            return entry != null
                && !string.IsNullOrEmpty(entry.FieldName)
                && !string.IsNullOrEmpty(entry.Type)
                && entry.GameObject != null;
        }

        private static string ResolveTargetBaseClause(UIAutoGenerator collector)
        {
            var targetType = ResolveTargetType(collector);
            if (targetType == null)
            {
                // Main class not compiled yet; partial will resolve once it exists.
                return string.Empty;
            }

            if (typeof(UIWindow).IsAssignableFrom(targetType) ||
                typeof(UIPanel).IsAssignableFrom(targetType) ||
                typeof(UIWidget).IsAssignableFrom(targetType))
            {
                return string.Empty;
            }

            Debug.LogWarning(
                $"[MUI] Target class '{targetType.FullName}' does not inherit from UIWindow, UIPanel, or UIWidget. Generated binding will not override AutoBindComponents.",
                collector);
            return string.Empty;
        }

        private static Type ResolveTargetType(UIAutoGenerator collector)
        {
            var fullName = string.IsNullOrEmpty(collector.TargetNamespace)
                ? collector.TargetClassName
                : $"{collector.TargetNamespace}.{collector.TargetClassName}";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            var parts = new System.Collections.Generic.List<string>();
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
