using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor
{
    [CustomEditor(typeof(UIBindingCollector))]
    public class UIBindingCollectorEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetClassName;
        private SerializedProperty _targetNamespace;
        private SerializedProperty _entries;

        private void OnEnable()
        {
            _targetClassName = serializedObject.FindProperty(nameof(UIBindingCollector.TargetClassName));
            _targetNamespace = serializedObject.FindProperty(nameof(UIBindingCollector.TargetNamespace));
            _entries = serializedObject.FindProperty(nameof(UIBindingCollector.Entries));

            AutoScanOnce((UIBindingCollector)target);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_targetClassName);
            EditorGUILayout.PropertyField(_targetNamespace);
            DrawEntries();

            serializedObject.ApplyModifiedProperties();

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
            ScanBindings(collector, rule);

            var outputDir = MUIConfig.GetGeneratedOutputPath();
            UIBindingCodeGenerator.Generate(collector, outputDir);
        }

        private static void AutoScanOnce(UIBindingCollector collector)
        {
            if (collector == null || collector.HasAutoScanned)
            {
                return;
            }

            var rule = UIGlobal.BindingNamingRule as UIBindingNamingRule ?? new DefaultUIBindingNamingRule();
            Undo.RecordObject(collector, "Auto Scan UI Bindings");
            ScanBindings(collector, rule);
            collector.HasAutoScanned = true;
            EditorUtility.SetDirty(collector);
        }

        private static void ScanBindings(UIBindingCollector collector, UIBindingNamingRule rule)
        {
            if (collector == null)
            {
                return;
            }

            ApplyTargetNaming(collector, rule);
            collector.Entries.Clear();
            ScanChildren(collector.transform, collector.transform, collector, rule);
            collector.HasAutoScanned = true;
            EditorUtility.SetDirty(collector);
        }

        public static void ApplyTargetNaming(UIBindingCollector collector, UIBindingNamingRule rule)
        {
            if (collector == null)
            {
                return;
            }

            rule ??= new DefaultUIBindingNamingRule();
            var context = UIBindingNamingContext.FromCollector(collector);
            if (string.IsNullOrEmpty(collector.TargetClassName))
            {
                collector.TargetClassName = rule.ToTargetClassName(context) ?? string.Empty;
            }

            if (string.IsNullOrEmpty(collector.TargetNamespace))
            {
                collector.TargetNamespace = rule.ToTargetNamespace(context) ?? string.Empty;
            }
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
                    collector.Entries.Add(new UIBindingEntry
                    {
                        FieldName = fieldName,
                        Type = componentType.FullName,
                        GameObject = child.gameObject,
                    });
                    if (!HasRequiredComponent(child.gameObject, componentType))
                    {
                        Debug.LogWarning(
                            $"[MUI] Binding '{fieldName}' expects component '{componentType.FullName}' on '{GetRelativePath(root, child)}', but it was not found.",
                            child.gameObject);
                    }
                }
                ScanChildren(root, child, collector, rule);
            }
        }

        private void DrawEntries()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Entries ({_entries.arraySize})", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        AddEntry();
                    }
                }

                if (_entries.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No binding entries. Scan children or add one manually.", MessageType.Info);
                    return;
                }

                for (var i = 0; i < _entries.arraySize; i++)
                {
                    var entry = _entries.GetArrayElementAtIndex(i);
                    DrawEntry(entry);
                }
            }
        }

        private void DrawEntry(SerializedProperty entry)
        {
            var fieldName = entry.FindPropertyRelative(nameof(UIBindingEntry.FieldName));
            var typeDropdown = entry.FindPropertyRelative(nameof(UIBindingEntry.Type));
            var gameObject = entry.FindPropertyRelative(nameof(UIBindingEntry.GameObject));
            var selectedGameObject = gameObject.objectReferenceValue as GameObject;
            var selectedType = ResolveType(typeDropdown.stringValue);
            var isMissingComponent = selectedGameObject != null && !HasRequiredComponent(selectedGameObject, selectedType);

            var rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f);
            rowRect = new Rect(
                rowRect.x + 2f,
                rowRect.y + 2f,
                rowRect.width - 4f,
                EditorGUIUtility.singleLineHeight);

            var fieldRect = new Rect(rowRect.x, rowRect.y, rowRect.width * 0.32f, rowRect.height);
            var typeRect = new Rect(fieldRect.xMax + 4f, rowRect.y, rowRect.width * 0.28f, rowRect.height);
            var objectRect = new Rect(typeRect.xMax + 4f, rowRect.y, rowRect.xMax - typeRect.xMax - 4f, rowRect.height);

            EditorGUI.PropertyField(fieldRect, fieldName, GUIContent.none);
            DrawTypeDropdown(typeRect, typeDropdown);
            EditorGUI.PropertyField(objectRect, gameObject, GUIContent.none);

            if (isMissingComponent)
            {
                EditorGUILayout.HelpBox(
                    $"'{selectedGameObject.name}' does not contain component '{GetTypeDisplayName(typeDropdown.stringValue)}'.",
                    MessageType.Warning);
            }
        }

        private void AddEntry()
        {
            _entries.InsertArrayElementAtIndex(_entries.arraySize);
            var entry = _entries.GetArrayElementAtIndex(_entries.arraySize - 1);
            entry.isExpanded = true;
            entry.FindPropertyRelative(nameof(UIBindingEntry.FieldName)).stringValue = string.Empty;
            entry.FindPropertyRelative(nameof(UIBindingEntry.Type)).stringValue = typeof(GameObject).FullName;
            entry.FindPropertyRelative(nameof(UIBindingEntry.GameObject)).objectReferenceValue = null;
        }

        private static void DrawTypeDropdown(Rect rect, SerializedProperty typeDropdown)
        {
            var options = GetTypeDropdownOptions();
            var values = options.Select(option => option.FullName).ToList();
            var labels = options.Select(option => option.Name).ToList();

            if (!string.IsNullOrEmpty(typeDropdown.stringValue) && !values.Contains(typeDropdown.stringValue))
            {
                values.Add(typeDropdown.stringValue);
                labels.Add(GetTypeDisplayName(typeDropdown.stringValue));
            }

            var selectedIndex = Mathf.Max(0, values.IndexOf(typeDropdown.stringValue));
            selectedIndex = EditorGUI.Popup(rect, selectedIndex, labels.ToArray());
            typeDropdown.stringValue = values[selectedIndex];
        }

        private static List<Type> GetTypeDropdownOptions()
        {
            var rule = UIGlobal.BindingNamingRule as UIBindingNamingRule ?? new DefaultUIBindingNamingRule();
            return rule.PrefixMap.Values
                .Concat(new[] { typeof(GameObject) })
                .Where(type => type != null)
                .Distinct()
                .OrderBy(type => type.Name)
                .ToList();
        }

        private static bool HasRequiredComponent(GameObject gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null)
            {
                return false;
            }

            if (componentType == typeof(GameObject))
            {
                return true;
            }

            return gameObject.GetComponent(componentType) != null;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            if (typeName == typeof(GameObject).FullName)
            {
                return typeof(GameObject);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string GetTypeDisplayName(string typeName)
        {
            var type = ResolveType(typeName);
            return type != null ? type.Name : typeName;
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
