using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor.Tests
{
    public class UIBindingNamingRuleTests
    {
        private const string TempPrefabFolder = "Assets/MUFramework/Tests/Editor/TempPrefabs";
        private const string TempPrefabPath = TempPrefabFolder + "/InventoryWindow.prefab";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempPrefabPath);
            AssetDatabase.DeleteAsset(TempPrefabFolder);
        }

        [Test]
        public void DefaultRuleGeneratesTargetClassNameFromRootGameObjectName()
        {
            var rule = new DefaultUIBindingNamingRule();
            var context = new UIGeneratorContext(
                "Inventory Window-Root",
                null,
                null);

            Assert.AreEqual("InventoryWindowRoot", rule.ToTargetClassName(context));
            Assert.AreEqual(string.Empty, rule.ToTargetNamespace(context));
        }

        [Test]
        public void ContextProvidesRootGameObjectNameAndPrefabDirectory()
        {
            if (!AssetDatabase.IsValidFolder(TempPrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/MUFramework/Tests/Editor", "TempPrefabs");
            }

            var root = new GameObject("InventoryWindow");
            try
            {
                var collector = root.AddComponent<UIAutoGenerator>();
                PrefabUtility.SaveAsPrefabAsset(root, TempPrefabPath);
                UnityEngine.Object.DestroyImmediate(root);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TempPrefabPath);
                collector = prefab.GetComponent<UIAutoGenerator>();

                var context = UIGeneratorContext.FromCollector(collector);

                Assert.AreEqual("InventoryWindow", context.RootGameObjectName);
                Assert.AreEqual(TempPrefabPath, context.PrefabAssetPath);
                Assert.AreEqual(TempPrefabFolder, context.PrefabDirectory);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void ApplyTargetNamingKeepsExistingCollectorTargetNames()
        {
            var root = new GameObject("InventoryWindow");
            try
            {
                var collector = root.AddComponent<UIAutoGenerator>();
                collector.TargetClassName = "OldClass";
                collector.TargetNamespace = "Old.Namespace";

                UIAutoGeneratorEditor.ApplyTargetNaming(collector, new FixedTargetNamingRule());

                Assert.AreEqual("OldClass", collector.TargetClassName);
                Assert.AreEqual("Old.Namespace", collector.TargetNamespace);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyTargetNamingFillsOnlyEmptyCollectorTargetNames()
        {
            var root = new GameObject("InventoryWindow");
            try
            {
                var collector = root.AddComponent<UIAutoGenerator>();
                collector.TargetClassName = string.Empty;
                collector.TargetNamespace = "Old.Namespace";

                UIAutoGeneratorEditor.ApplyTargetNaming(collector, new FixedTargetNamingRule());

                Assert.AreEqual("GeneratedClass", collector.TargetClassName);
                Assert.AreEqual("Old.Namespace", collector.TargetNamespace);

                collector.TargetClassName = "OldClass";
                collector.TargetNamespace = string.Empty;

                UIAutoGeneratorEditor.ApplyTargetNaming(collector, new FixedTargetNamingRule());

                Assert.AreEqual("OldClass", collector.TargetClassName);
                Assert.AreEqual("Generated.Namespace", collector.TargetNamespace);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CustomRuleCanGenerateTargetNamesFromContext()
        {
            var rule = new PrefabDirectoryNamingRule();
            var context = new UIGeneratorContext(
                "InventoryWindow",
                "Assets/Game/UI/Windows/InventoryWindow.prefab",
                "Assets/Game/UI/Windows");

            Assert.AreEqual("InventoryWindowBinder", rule.ToTargetClassName(context));
            Assert.AreEqual("Game.UI.Windows", rule.ToTargetNamespace(context));
        }

        private sealed class PrefabDirectoryNamingRule : UIBindingNamingRule
        {
            public override IReadOnlyDictionary<string, Type> PrefixMap { get; } = new Dictionary<string, Type>();

            public override string ToTargetClassName(UIGeneratorContext context)
            {
                return context.RootGameObjectName + "Binder";
            }

            public override string ToTargetNamespace(UIGeneratorContext context)
            {
                return context.PrefabDirectory.Replace("Assets/", "").Replace("/", ".");
            }
        }

        private sealed class FixedTargetNamingRule : UIBindingNamingRule
        {
            public override IReadOnlyDictionary<string, Type> PrefixMap { get; } = new Dictionary<string, Type>();

            public override string ToTargetClassName(UIGeneratorContext context)
            {
                return "GeneratedClass";
            }

            public override string ToTargetNamespace(UIGeneratorContext context)
            {
                return "Generated.Namespace";
            }
        }
    }
}
