using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public sealed class MUIConfigWindow : EditorWindow
    {
        private MUIConfig _config;
        private SerializedObject _serializedConfig;
        private SerializedProperty _assetSavePath;
        private SerializedProperty _generatedBindOutputPath;
        private SerializedProperty _generatedScriptOutputPath;
        private SerializedProperty _defaultNamespace;

        [MenuItem("Tools/MUFramework/MUI 配置")]
        public static void Open()
        {
            GetWindow<MUIConfigWindow>("MUI 配置");
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                LoadConfig();
            }

            EditorGUILayout.LabelField("MUI 代码生成配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _serializedConfig.Update();

            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_assetSavePath, new GUIContent("资源保存路径", "UIBindingCollector 等 Editor 资源的保存目录"));
            _assetSavePath.stringValue = MUIConfig.NormalizeAssetPath(
                _assetSavePath.stringValue,
                MUIConfig.DefaultAssetSavePath);

            EditorGUILayout.PropertyField(_generatedScriptOutputPath, new GUIContent("脚本生成路径", "主类脚本（不存在时自动生成）的输出目录"));
            _generatedScriptOutputPath.stringValue = MUIConfig.NormalizeAssetPath(
                _generatedScriptOutputPath.stringValue,
                MUIConfig.DefaultGeneratedScriptOutputPath);

            EditorGUILayout.PropertyField(_generatedBindOutputPath, new GUIContent("绑定生成路径", "自动生成的 .AutoBind.cs 文件的输出目录"));
            _generatedBindOutputPath.stringValue = MUIConfig.NormalizeAssetPath(
                _generatedBindOutputPath.stringValue,
                MUIConfig.DefaultGeneratedBindOutputPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("命名设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultNamespace, new GUIContent("默认命名空间", "新建 UIBindingCollector 时自动填入的命名空间，留空表示全局命名空间"));

            _serializedConfig.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "脚本生成路径：主类文件（ClassName.cs），不存在时在此目录自动创建。\n" +
                "绑定生成路径：自动生成的绑定部分类（ClassName.AutoBind.cs）。",
                MessageType.Info);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("恢复默认"))
                {
                    Undo.RecordObject(_config, "Reset MUI Config");
                    _config.AssetSavePath = MUIConfig.DefaultAssetSavePath;
                    _config.GeneratedBindOutputPath = MUIConfig.DefaultGeneratedBindOutputPath;
                    _config.GeneratedScriptOutputPath = MUIConfig.DefaultGeneratedScriptOutputPath;
                    _config.DefaultNamespaceValue = MUIConfig.DefaultNamespace;
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                    LoadConfig();
                }

                if (GUILayout.Button("选中配置资源"))
                {
                    Selection.activeObject = _config;
                    EditorGUIUtility.PingObject(_config);
                }
            }
        }

        private void LoadConfig()
        {
            _config = MUIConfig.GetOrCreate();
            _serializedConfig = new SerializedObject(_config);
            _assetSavePath = _serializedConfig.FindProperty("_assetSavePath");
            _generatedBindOutputPath = _serializedConfig.FindProperty("_generatedBindOutputPath");
            _generatedScriptOutputPath = _serializedConfig.FindProperty("_generatedScriptOutputPath");
            _defaultNamespace = _serializedConfig.FindProperty("_defaultNamespace");
        }
    }
}
