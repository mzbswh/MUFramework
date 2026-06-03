using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    public sealed class MUIConfigWindow : EditorWindow
    {
        private MUIConfig _config;
        private SerializedObject _serializedConfig;
        private SerializedProperty _assetSavePath;
        private SerializedProperty _generatedOutputPath;

        [MenuItem("Tools/MUFramework/MUI Config")]
        public static void Open()
        {
            GetWindow<MUIConfigWindow>("MUI Config");
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

            EditorGUILayout.LabelField("MUI Code Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _serializedConfig.Update();
            EditorGUILayout.PropertyField(_assetSavePath, new GUIContent("Asset Save Path"));
            _assetSavePath.stringValue = MUIConfig.NormalizeAssetPath(
                _assetSavePath.stringValue,
                MUIConfig.DefaultAssetSavePath);
            EditorGUILayout.PropertyField(_generatedOutputPath, new GUIContent("Generated Output Path"));
            _generatedOutputPath.stringValue = MUIConfig.NormalizeAssetPath(_generatedOutputPath.stringValue);
            _serializedConfig.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(
                "MUI assets and generated binding files are written to these Assets-relative directories.",
                MessageType.Info);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Default"))
                {
                    Undo.RecordObject(_config, "Reset MUI Config Paths");
                    _config.AssetSavePath = MUIConfig.DefaultAssetSavePath;
                    _config.GeneratedOutputPath = MUIConfig.DefaultGeneratedOutputPath;
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                    LoadConfig();
                }

                if (GUILayout.Button("Select Config Asset"))
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
            _generatedOutputPath = _serializedConfig.FindProperty("_generatedOutputPath");
        }
    }
}
