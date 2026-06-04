using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public static class UICodeGenerator
    {
        /// <summary>
        /// Generates the main class file if it does not already exist on disk.
        /// Returns true if a file was written.
        /// </summary>
        public static bool GenerateIfMissing(UIAutoGenerator collector, string outputDir)
        {
            if (collector == null || string.IsNullOrEmpty(collector.TargetClassName)) return false;

            outputDir = MUIConfig.NormalizeAssetPath(outputDir);
            var filePath = Path.Combine(outputDir, $"{collector.TargetClassName}.cs");

            if (File.Exists(filePath)) return false;

            var baseTypeName = ResolveBaseTypeName(collector.TargetBaseType);

            var sb = new StringBuilder();
            sb.AppendLine("using MUFramework;");
            sb.AppendLine();

            bool hasNamespace = !string.IsNullOrEmpty(collector.TargetNamespace);
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {collector.TargetNamespace}");
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";
            sb.AppendLine($"{indent}public partial class {collector.TargetClassName} : {baseTypeName}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", outputDir));
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[MUI] Generated main class: {filePath}");
            return true;
        }

        private static string ResolveBaseTypeName(UIBindingTargetType targetType)
        {
            return targetType switch
            {
                UIBindingTargetType.Panel => "UIPanel",
                UIBindingTargetType.Widget => "UIWidget",
                _ => "UIWindow",
            };
        }
    }
}
