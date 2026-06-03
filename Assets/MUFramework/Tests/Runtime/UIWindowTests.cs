using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    /// <summary>
    /// 测试 UIWindow 的生命周期调用顺序和状态转换。
    /// UIWindow 是纯 C# 类，通过 UIStackNode 驱动，不依赖 MonoBehaviour。
    /// </summary>
    public class UIWindowTests
    {
        private TestWindow _window;
        private UIStackNode _node;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            var config = WindowOpenConfig.Default;
            config.WindowId = "TestWindow";

            _window = new TestWindow();
            _node = new UIStackNode();
            _node.Initialize(config, _window, 1L);

            _go = new GameObject("TestWindow");
            _node.AttachGameObject(_go);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // 将 window 绑定到 node（模拟 UIManager.OpenCore 的行为）
        private void InitWindow(params object[] args)
        {
            _window.Init(_node);
            _window.OnOpenInternal(args);
        }

        // ===== Init / OnCreate =====

        [Test]
        public void Init_FirstCall_InvokesOnCreate()
        {
            _window.Init(_node);
            Assert.AreEqual(1, _window.OnCreateCount);
        }

        [Test]
        public void Init_SecondCall_DoesNotInvokeOnCreateAgain()
        {
            _window.Init(_node);
            _window.Init(_node);
            Assert.AreEqual(1, _window.OnCreateCount);
        }

        // ===== Open =====

        [Test]
        public void Open_InvokesOnOpen()
        {
            InitWindow("arg1", 42);
            Assert.AreEqual(1, _window.OnOpenCount);
        }

        [Test]
        public void Open_PassesArgsToOnOpen()
        {
            InitWindow("hello", 99);
            Assert.AreEqual(2, _window.LastOpenArgs.Length);
            Assert.AreEqual("hello", _window.LastOpenArgs[0]);
            Assert.AreEqual(99, _window.LastOpenArgs[1]);
        }

        // ===== Show / Hide =====

        [Test]
        public void Show_NoAnimation_InvokesOnShow()
        {
            InitWindow();
            _window.Show(withAnimation: false);
            Assert.AreEqual(1, _window.OnShowCount);
        }

        [Test]
        public void Show_NoAnimation_ActivatesGameObject()
        {
            InitWindow();
            _go.SetActive(false);
            _window.Show(withAnimation: false);
            Assert.IsTrue(_go.activeSelf);
        }

        [Test]
        public void Hide_NoAnimation_InvokesOnHide()
        {
            InitWindow();
            _window.Show(withAnimation: false);
            _window.Hide(withAnimation: false);
            Assert.AreEqual(1, _window.OnHideCount);
        }

        [Test]
        public void Hide_NoAnimation_DeactivatesGameObject()
        {
            InitWindow();
            _window.Show(withAnimation: false);
            _window.Hide(withAnimation: false);
            Assert.IsFalse(_go.activeSelf);
        }

        // ===== Pause / Resume =====

        [Test]
        public void Pause_InvokesOnPause()
        {
            InitWindow();
            _node.SetPause(true);
            Assert.AreEqual(1, _window.OnPauseCount);
        }

        [Test]
        public void Pause_SetsPausedState()
        {
            InitWindow();
            _node.SetPause(true);
            Assert.IsTrue(_window.IsPause);
        }

        [Test]
        public void Pause_Idempotent_NotCalledTwice()
        {
            InitWindow();
            _node.SetPause(true);
            _node.SetPause(true);
            Assert.AreEqual(1, _window.OnPauseCount);
        }

        [Test]
        public void Resume_AfterPause_InvokesOnResume()
        {
            InitWindow();
            _node.SetPause(true);
            _node.SetPause(false);
            Assert.AreEqual(1, _window.OnResumeCount);
        }

        [Test]
        public void Resume_ClearsPausedState()
        {
            InitWindow();
            _node.SetPause(true);
            _node.SetPause(false);
            Assert.IsFalse(_window.IsPause);
        }

        [Test]
        public void Resume_WithoutPause_DoesNothing()
        {
            InitWindow();
            _node.SetPause(false);
            Assert.AreEqual(0, _window.OnResumeCount);
        }

        // ===== Update =====

        [Test]
        public void Update_CallsDeltaTime_NoException()
        {
            InitWindow();
            Assert.DoesNotThrow(() => _window.Update(0.016f));
        }

        [Test]
        public void UpdatePerSecond_NoException()
        {
            InitWindow();
            Assert.DoesNotThrow(() => _window.UpdatePerSecond());
        }

        // ===== CompleteClose =====

        [Test]
        public void CompleteClose_InvokesOnClose()
        {
            InitWindow();
            _window.CompleteClose();
            Assert.AreEqual(1, _window.OnCloseCount);
        }

        // ===== Destroy =====

        [Test]
        public void Destroy_InvokesOnDestroy()
        {
            InitWindow();
            _window.Destroy();
            Assert.AreEqual(1, _window.OnDestroyCount);
        }

        // ===== SetInteractable =====

        [Test]
        public void SetInteractable_False_BlocksRaycastsFalse()
        {
            InitWindow();
            _window.SetInteractable(false);
            Assert.IsFalse(_node.CanvasGroup.interactable);
            Assert.IsFalse(_node.CanvasGroup.blocksRaycasts);
        }

        [Test]
        public void SetInteractable_True_EnablesInteraction()
        {
            InitWindow();
            _window.SetInteractable(false);
            _window.SetInteractable(true);
            Assert.IsTrue(_node.CanvasGroup.interactable);
            Assert.IsTrue(_node.CanvasGroup.blocksRaycasts);
        }

        // ===== AttachWidget =====

        [Test]
        public void AttachWidget_WidgetAddedOnce()
        {
            InitWindow();
            var widgetGo = new GameObject("Widget");
            var widget = new TestWidget();
            widget.Init(widgetGo);
            _window.AttachWidget(widget);
            _window.AttachWidget(widget); // 重复添加应忽略

            // 验证 Update 只传递一次（无异常即通过，数量通过 Widget 内部计数验证）
            Assert.DoesNotThrow(() => _window.Update(0.016f));
            Assert.AreEqual(1, widget.UpdateCount);

            Object.DestroyImmediate(widgetGo);
        }

        // ===== Widget Lifecycle Notification =====

        [Test]
        public void Widget_NotifyOpen_CalledWhenWindowOpens()
        {
            InitWindow();
            var widgetGo = new GameObject("Widget");
            var widget = new TestWidget();
            widget.Init(widgetGo);
            _window.AttachWidget(widget);

            _window.OnOpenInternal_ForTest(System.Array.Empty<object>());

            Assert.AreEqual(1, widget.OpenCount);
            Object.DestroyImmediate(widgetGo);
        }

        [Test]
        public void Widget_NotifyShow_CalledWhenWindowShows()
        {
            InitWindow();
            var widgetGo = new GameObject("Widget");
            var widget = new TestWidget();
            widget.Init(widgetGo);
            _window.AttachWidget(widget);

            _window.Show(withAnimation: false);

            Assert.AreEqual(1, widget.ShowCount);
            Object.DestroyImmediate(widgetGo);
        }

        [Test]
        public void Widget_NotifyPause_CalledWhenWindowPauses()
        {
            InitWindow();
            var widgetGo = new GameObject("Widget");
            var widget = new TestWidget();
            widget.Init(widgetGo);
            _window.AttachWidget(widget);

            _window.OnPauseInternal_ForTest(true);

            Assert.AreEqual(1, widget.PauseCount);
            Object.DestroyImmediate(widgetGo);
        }

        // ===== 属性委托到 UIStackNode =====

        [Test]
        public void UniqueId_MatchesNodeUniqueId()
        {
            _window.Init(_node);
            Assert.AreEqual(_node.UniqueId, _window.UniqueId);
        }

        [Test]
        public void IsClosed_InitiallyFalse()
        {
            _window.Init(_node);
            Assert.IsFalse(_window.IsClosed);
        }
    }

}
