using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    public class CommonUtilsTests
    {
        // ===== HasState =====

        [Test]
        public void HasState_FlagPresent_ReturnsTrue()
        {
            var state = UIState.Paused | UIState.Hidden;
            Assert.IsTrue(state.HasState(UIState.Paused));
            Assert.IsTrue(state.HasState(UIState.Hidden));
        }

        [Test]
        public void HasState_FlagAbsent_ReturnsFalse()
        {
            var state = UIState.Paused;
            Assert.IsFalse(state.HasState(UIState.Hidden));
            Assert.IsFalse(state.HasState(UIState.Closed));
        }

        [Test]
        public void HasState_None_ReturnsFalse()
        {
            Assert.IsFalse(UIState.None.HasState(UIState.Paused));
        }

        [Test]
        public void HasState_MultipleFlagsPartialMatch_ReturnsTrue()
        {
            var state = UIState.Paused | UIState.Covered;
            Assert.IsTrue(state.HasState(UIState.Paused));
            Assert.IsFalse(state.HasState(UIState.Hidden));
        }

        // ===== GetOrAddComponent (GameObject) =====

        [Test]
        public void GetOrAddComponent_ComponentNotPresent_AddsAndReturns()
        {
            var go = new GameObject("test");
            var canvas = go.GetOrAddComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(1, go.GetComponents<Canvas>().Length);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetOrAddComponent_ComponentAlreadyPresent_ReturnsSameInstance()
        {
            var go = new GameObject("test");
            var canvas1 = go.AddComponent<Canvas>();
            var canvas2 = go.GetOrAddComponent<Canvas>();
            Assert.AreSame(canvas1, canvas2);
            Assert.AreEqual(1, go.GetComponents<Canvas>().Length);
            Object.DestroyImmediate(go);
        }

        // ===== GetOrAddComponent (Component) =====

        [Test]
        public void GetOrAddComponent_OnComponent_DelegatesToGameObject()
        {
            var go = new GameObject("test");
            var transform = go.transform;
            var canvas = transform.GetOrAddComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Object.DestroyImmediate(go);
        }
    }
}
