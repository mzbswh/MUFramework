using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    public class UIPanelTests
    {
        private GameObject _panelGO;

        [SetUp]
        public void SetUp()
        {
            _panelGO = new GameObject("Panel");
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGO != null)
            {
                Object.DestroyImmediate(_panelGO);
            }
        }

        [Test]
        public void Init_CallsOnCreate()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            Assert.AreEqual(1, panel.CreateCount);
        }

        [Test]
        public void Activate_SetsActiveTrue_CallsOnActivate()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            panel.Activate();
            Assert.IsTrue(panel.IsActive);
            Assert.IsTrue(_panelGO.activeSelf);
            Assert.AreEqual(1, panel.ActivateCount);
        }

        [Test]
        public void Activate_Twice_OnlyCallsOnActivateOnce()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            panel.Activate();
            panel.Activate();
            Assert.AreEqual(1, panel.ActivateCount);
        }

        [Test]
        public void Deactivate_SetsActiveFalse_CallsOnDeactivate()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            panel.Activate();
            panel.Deactivate();
            Assert.IsFalse(panel.IsActive);
            Assert.IsFalse(_panelGO.activeSelf);
            Assert.AreEqual(1, panel.DeactivateCount);
        }

        [Test]
        public void Deactivate_WhenNotActive_DoesNothing()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            panel.Deactivate();
            Assert.AreEqual(0, panel.DeactivateCount);
        }

        [Test]
        public void Destroy_ClearsReferences_CallsOnDestroy()
        {
            var panel = new TestPanel();
            panel.Init(_panelGO, null);
            panel.Destroy();
            Assert.IsNull(panel.GameObject);
            Assert.AreEqual(1, panel.DestroyCount);
        }

        [Test]
        public void GenericPanel_Activate_PassesData()
        {
            var panel = new TestDataPanel();
            panel.Init(_panelGO, null);
            panel.Activate(42);
            Assert.AreEqual(42, panel.LastData);
        }

        [Test]
        public void SwitchPanel_DeactivatesOld_ActivatesNew()
        {
            var goA = new GameObject("A");
            var goB = new GameObject("B");
            var panelA = new TestPanel();
            var panelB = new TestPanel();
            panelA.Init(goA, null);
            panelB.Init(goB, null);

            UIPanel current = null;
            void Switch(UIPanel next)
            {
                if (current == next) return;
                current?.Deactivate();
                current = next;
                current.Activate();
            }

            Switch(panelA);
            Assert.IsTrue(panelA.IsActive);
            Assert.IsFalse(panelB.IsActive);

            Switch(panelB);
            Assert.IsFalse(panelA.IsActive);
            Assert.IsTrue(panelB.IsActive);

            Object.DestroyImmediate(goA);
            Object.DestroyImmediate(goB);
        }
    }
}
