using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    public class UIStackNodeTests
    {
        private UIStackNode _node;
        private WindowOpenConfig _config;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _config = WindowOpenConfig.Default;
            _config.WindowId = "TestWindow";
            _node = new UIStackNode();
            _node.Initialize(_config, new TestWindow(), 1L);

            _go = new GameObject("TestWindow");
            _node.AttachGameObject(_go);
            _node.Window.Init(_node);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ===== Initialize =====

        [Test]
        public void Initialize_SetsWindowId()
        {
            Assert.AreEqual("TestWindow", _node.WindowId);
        }

        [Test]
        public void Initialize_SetsUniqueId()
        {
            Assert.AreEqual(1L, _node.UniqueId);
        }

        [Test]
        public void Initialize_StateIsLoading()
        {
            var freshNode = new UIStackNode();
            freshNode.Initialize(_config, new TestWindow(), 99L);
            Assert.IsTrue(freshNode.IsLoading);
            Assert.IsFalse(freshNode.IsLoaded);
        }

        // ===== AttachGameObject =====

        [Test]
        public void AttachGameObject_SetsGameObject()
        {
            Assert.AreSame(_go, _node.GameObject);
        }

        [Test]
        public void AttachGameObject_TransitionsToLoaded()
        {
            Assert.IsTrue(_node.IsLoaded);
            Assert.IsFalse(_node.IsLoading);
        }

        [Test]
        public void AttachGameObject_AddsCanvasGroup()
        {
            Assert.IsNotNull(_node.CanvasGroup);
        }

        [Test]
        public void AttachGameObject_AddsCanvas()
        {
            Assert.IsNotNull(_node.Canvas);
        }

        // ===== SetUniqueId =====

        [Test]
        public void SetUniqueId_UpdatesId()
        {
            _node.SetUniqueId(42L);
            Assert.AreEqual(42L, _node.UniqueId);
        }

        // ===== SetState / UnsetState =====

        [Test]
        public void SetState_AddsFlag()
        {
            _node.SetState(UIState.Paused);
            Assert.IsTrue(_node.IsPause);
        }

        [Test]
        public void UnsetState_RemovesFlag()
        {
            _node.SetState(UIState.Paused);
            _node.UnsetState(UIState.Paused);
            Assert.IsFalse(_node.IsPause);
        }

        [Test]
        public void SetState_DoesNotAffectOtherFlags()
        {
            _node.SetState(UIState.Paused);
            Assert.IsFalse(_node.IsHidden);
        }

        // ===== SetClosedState =====

        [Test]
        public void SetClosedState_SetsClosed()
        {
            _node.SetState(UIState.Closing);
            _node.SetClosedState();
            Assert.IsTrue(_node.IsClosed);
            Assert.IsFalse(_node.IsClosing);
            Assert.IsTrue(_node.IsPause);
            Assert.IsTrue(_node.IsHidden);
        }

        // ===== SetHide =====

        [Test]
        public void SetHide_True_SetsHiddenFlag()
        {
            _node.SetHide(true);
            Assert.IsTrue(_node.IsHidden);
        }

        [Test]
        public void SetHide_False_ClearsHiddenFlag()
        {
            _node.SetHide(true);
            _node.SetHide(false);
            Assert.IsFalse(_node.IsHidden);
        }

        [Test]
        public void SetHide_Idempotent_NoDuplicateCall()
        {
            _node.SetHide(true);
            _node.SetHide(true); // second call should be no-op
            Assert.IsTrue(_node.IsHidden);
        }

        // ===== SetPause =====

        [Test]
        public void SetPause_True_SetsPausedFlag()
        {
            _node.SetPause(true);
            Assert.IsTrue(_node.IsPause);
        }

        [Test]
        public void SetPause_False_ClearsPausedFlag()
        {
            _node.SetPause(true);
            _node.SetPause(false);
            Assert.IsFalse(_node.IsPause);
        }

        // ===== SetCover =====

        [Test]
        public void SetCover_True_SetsCoveredFlag()
        {
            _node.SetCover(true);
            Assert.IsTrue(_node.IsCovered);
        }

        [Test]
        public void SetCover_False_ClearsCoveredFlag()
        {
            _node.SetCover(true);
            _node.SetCover(false);
            Assert.IsFalse(_node.IsCovered);
        }

        // ===== SetExpireTime =====

        [Test]
        public void SetExpireTime_StoresValue()
        {
            _node.SetExpireTime(100.0);
            Assert.AreEqual(100.0, _node.ExpireTime);
        }

        // ===== SetOrder =====

        [Test]
        public void SetOrder_UpdatesCanvasSortingOrder()
        {
            _node.SetOrder(50);
            Assert.AreEqual(50, _node.Canvas.sortingOrder);
        }

        [Test]
        public void SetOrder_WhenCanvasNull_DoesNotThrow()
        {
            var emptyNode = new UIStackNode();
            emptyNode.Initialize(_config, new TestWindow(), 2L);
            // Canvas is null (AttachGameObject not called)
            Assert.DoesNotThrow(() => emptyNode.SetOrder(10));
        }

        // ===== Dispose / Clear =====

        [Test]
        public void Dispose_ClearsAllFields()
        {
            _node.Dispose();
            Assert.AreEqual(0L, _node.UniqueId);
            Assert.IsNull(_node.WindowId);
            Assert.IsNull(_node.Window);
            Assert.IsNull(_node.GameObject);
        }
    }
}
