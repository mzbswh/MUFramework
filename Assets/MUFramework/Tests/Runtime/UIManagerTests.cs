using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MUFramework;

namespace MUFramework.Tests
{
    /// <summary>
    /// UIManager 集成测试。
    /// 每个测试都在干净的 UIManager 实例上运行（通过 TearDown 销毁）。
    /// </summary>
    public class UIManagerTests
    {
        private UIManager _manager;
        private TestLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            UIGlobal.LogHandler = _logger.Log;
            UIGlobal.GetWindowClassTypeFunc = TestWindowFactory.GetType;
            UIGlobal.GetWindowOpenConfigFunc = TestWindowFactory.GetConfig;

            _manager = UIManager.Instance;
            _manager.SetResourceLoader(new TestResourceLoader());
            _manager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // 销毁 UIManager GameObject，使下一个测试获得全新实例
            if (_manager != null)
            {
                Object.DestroyImmediate(_manager.gameObject);
            }
            UIGlobal.LogHandler = null;
            UIGlobal.GetWindowClassTypeFunc = null;
            UIGlobal.GetWindowOpenConfigFunc = null;
        }

        private WindowOpenConfig MakeConfig(string windowId,
            bool multiInstance = false,
            int maxInstances = 1,
            OverflowPolicy overflow = OverflowPolicy.CloseOldest,
            CacheType cache = CacheType.None,
            OpenBehavior openBehavior = OpenBehavior.KeepBelow,
            CoveredBehavior whenCovered = CoveredBehavior.Normal)
        {
            return new WindowOpenConfig
            {
                WindowId = windowId,
                AllowMultiInstance = multiInstance,
                MaxInstances = maxInstances,
                OverflowPolicy = overflow,
                CacheType = cache,
                OpenBehavior = openBehavior,
                WhenCovered = whenCovered,
                Layer = UILayer.Default,
            };
        }

        // ===== Initialize =====

        [Test]
        public void Initialize_Idempotent_NoDoubleInit()
        {
            _manager.Initialize(); // 第二次调用不应抛出
            Assert.Pass();
        }

        [Test]
        public void Initialize_CreatesUICamera()
        {
            Assert.IsNotNull(_manager.UICamera);
        }

        [Test]
        public void Initialize_CreatesCanvasRoot()
        {
            Assert.IsNotNull(_manager.CanvasRoot);
        }

        // ===== Open (同步) =====

        [Test]
        public void Open_ValidConfig_ReturnsWindow()
        {
            var window = _manager.Open(MakeConfig("TestWindow"));
            Assert.IsNotNull(window);
            Assert.IsInstanceOf<TestWindow>(window);
        }

        [Test]
        public void Open_ValidConfig_WindowExistsInManager()
        {
            var window = _manager.Open(MakeConfig("TestWindow"));
            Assert.IsTrue(_manager.ExistUI(window.UniqueId));
        }

        [Test]
        public void Open_ValidConfig_ExistUIByWindowId()
        {
            _manager.Open(MakeConfig("TestWindow"));
            Assert.IsTrue(_manager.ExistUI("TestWindow"));
        }

        [Test]
        public void Open_NullResourceLoader_ReturnsNull()
        {
            _manager.SetResourceLoader(null);
            var window = _manager.Open(MakeConfig("TestWindow"));
            Assert.IsNull(window);
        }

        [Test]
        public void Open_InvokesOnCreateOnce()
        {
            var window = _manager.Open(MakeConfig("TestWindow")) as TestWindow;
            Assert.AreEqual(1, window.OnCreateCount);
        }

        [Test]
        public void Open_InvokesOnOpen()
        {
            var window = _manager.Open(MakeConfig("TestWindow"), "arg1") as TestWindow;
            Assert.AreEqual(1, window.OnOpenCount);
        }

        [Test]
        public void Open_InvokesOnShow()
        {
            var window = _manager.Open(MakeConfig("TestWindow")) as TestWindow;
            Assert.AreEqual(1, window.OnShowCount);
        }

        // ===== Close =====

        [Test]
        public void Close_ByUniqueId_WindowNoLongerExists()
        {
            var window = _manager.Open(MakeConfig("TestWindow"));
            _manager.Close(window.UniqueId, withAnimation: false);
            Assert.IsFalse(_manager.ExistUI(window.UniqueId));
        }

        [Test]
        public void Close_ByWindowId_AllInstancesClosed()
        {
            var c = MakeConfig("TestWindow", multiInstance: true, maxInstances: 3);
            _manager.Open(c);
            _manager.Open(c);
            _manager.Close("TestWindow", closeAllInstances: true, withAnimation: false);
            Assert.IsFalse(_manager.ExistUI("TestWindow"));
        }

        [Test]
        public void Close_ByWindowId_OnlyOldestClosed_WhenCloseAllFalse()
        {
            var c = MakeConfig("TestWindow", multiInstance: true, maxInstances: 3);
            var w1 = _manager.Open(c);
            _manager.Open(c);
            _manager.Close("TestWindow", closeAllInstances: false, withAnimation: false);
            Assert.IsFalse(_manager.ExistUI(w1.UniqueId));
            Assert.IsTrue(_manager.ExistUI("TestWindow")); // 还剩1个
        }

        [Test]
        public void Close_NonExistentId_NoException()
        {
            Assert.DoesNotThrow(() => _manager.Close(99999L, withAnimation: false));
        }

        [Test]
        public void Close_NullWindowId_NoException()
        {
            Assert.DoesNotThrow(() => _manager.Close("NonExistent", withAnimation: false));
        }

        [Test]
        public void Close_InvokesOnCloseCallback()
        {
            bool called = false;
            var window = _manager.Open(MakeConfig("TestWindow"));
            _manager.Close(window.UniqueId, withAnimation: false, onComplete: () => called = true);
            Assert.IsTrue(called);
        }

        // ===== ExistUI =====

        [Test]
        public void ExistUI_AfterOpen_ReturnsTrue()
        {
            _manager.Open(MakeConfig("TestWindow"));
            Assert.IsTrue(_manager.ExistUI("TestWindow"));
        }

        [Test]
        public void ExistUI_BeforeOpen_ReturnsFalse()
        {
            Assert.IsFalse(_manager.ExistUI("TestWindow"));
        }

        // ===== Multi-instance =====

        [Test]
        public void Open_SingleInstance_SecondOpenReplacesFirst_CloseOldest()
        {
            var c = MakeConfig("TestWindow", multiInstance: false, overflow: OverflowPolicy.CloseOldest);
            var w1 = _manager.Open(c);
            var w2 = _manager.Open(c);
            Assert.IsNotNull(w2);
            Assert.IsFalse(_manager.ExistUI(w1.UniqueId));
            Assert.IsTrue(_manager.ExistUI(w2.UniqueId));
        }

        [Test]
        public void Open_SingleInstance_Reject_ReturnsNull()
        {
            var c = MakeConfig("TestWindow", multiInstance: false, overflow: OverflowPolicy.Reject);
            _manager.Open(c);
            var w2 = _manager.Open(c);
            Assert.IsNull(w2);
        }

        [Test]
        public void Open_MultiInstance_AllowsMultiple()
        {
            var c = MakeConfig("TestWindow", multiInstance: true, maxInstances: 3);
            var w1 = _manager.Open(c);
            var w2 = _manager.Open(c);
            Assert.IsNotNull(w1);
            Assert.IsNotNull(w2);
            Assert.AreNotEqual(w1.UniqueId, w2.UniqueId);
        }

        [Test]
        public void Open_MultiInstance_OverflowCloseNewest()
        {
            var c = MakeConfig("TestWindow", multiInstance: true, maxInstances: 2, overflow: OverflowPolicy.CloseNewest);
            var w1 = _manager.Open(c);
            var w2 = _manager.Open(c);
            var w3 = _manager.Open(c); // 应关闭 w2（最新），打开 w3
            Assert.IsTrue(_manager.ExistUI(w1.UniqueId));
            Assert.IsFalse(_manager.ExistUI(w2.UniqueId));
            Assert.IsTrue(_manager.ExistUI(w3.UniqueId));
        }

        // ===== Cache =====

        [Test]
        public void Close_PersistentCache_WindowCanBeReopened()
        {
            var c = MakeConfig("TestWindow", cache: CacheType.Persistent);
            var w1 = _manager.Open(c);
            var goRef = w1.GameObject;
            _manager.Close(w1.UniqueId, withAnimation: false);

            // 重新打开 —— 应该复用 GameObject
            var w2 = _manager.Open(c);
            Assert.IsNotNull(w2);
            Assert.AreSame(goRef, w2.GameObject); // 同一个 GameObject 实例
        }

        [Test]
        public void Close_NoCache_GameObjectDestroyed()
        {
            var c = MakeConfig("TestWindow", cache: CacheType.None);
            var w1 = _manager.Open(c);
            var goRef = w1.GameObject;
            _manager.Close(w1.UniqueId, withAnimation: false);

            // GameObject 应被销毁
            Assert.IsTrue(goRef == null);
        }

        // ===== HandleBackKey =====

        [Test]
        public void HandleBackKey_WindowOpen_ReturnsTrue()
        {
            _manager.Open(MakeConfig("TestWindow"));
            Assert.IsTrue(_manager.HandleBackKey());
        }

        [Test]
        public void HandleBackKey_NoWindows_ReturnsFalse()
        {
            Assert.IsFalse(_manager.HandleBackKey());
        }

        [Test]
        public void HandleBackKey_SkipBackKeyWindow_SkipsAndClosesNext()
        {
            var bottom = _manager.Open(MakeConfig("TestWindow"));
            var topConfig = MakeConfig("TestWindow2");
            topConfig.WindowAttr = WindowAttr.SkipBackKey;
            var top = _manager.Open(topConfig);

            Assert.IsTrue(_manager.HandleBackKey());
            Assert.IsFalse(_manager.ExistUI(bottom.UniqueId));
            Assert.IsTrue(_manager.ExistUI(top.UniqueId));
        }

        // ===== Dependencies =====

        [Test]
        public void Open_MissingDependency_NotOpen_ReturnsNull()
        {
            var c = MakeConfig("TestWindow");
            c.Dependencies = new System.Collections.Generic.List<string> { "TestWindow2" };
            c.DependencyMissingPolicy = DependencyMissingPolicy.NotOpen;
            var window = _manager.Open(c);
            Assert.IsNull(window);
        }

        [Test]
        public void Open_MissingDependency_OpenAnyway_Opens()
        {
            var c = MakeConfig("TestWindow");
            c.Dependencies = new System.Collections.Generic.List<string> { "TestWindow2" };
            c.DependencyMissingPolicy = DependencyMissingPolicy.OpenAnyway;
            var window = _manager.Open(c);
            Assert.IsNotNull(window);
        }

        [Test]
        public void Open_MissingDependency_OpenMissingDependency_OpensBoth()
        {
            var c = MakeConfig("TestWindow");
            c.Dependencies = new System.Collections.Generic.List<string> { "TestWindow2" };
            c.DependencyMissingPolicy = DependencyMissingPolicy.OpenMissingDependency;
            _manager.Open(c);
            Assert.IsTrue(_manager.ExistUI("TestWindow"));
            Assert.IsTrue(_manager.ExistUI("TestWindow2"));
        }

        // ===== OpenAsync =====

        [UnityTest]
        public IEnumerator OpenAsync_CallsCallbackWithWindow()
        {
            UIWindow result = null;
            _manager.OpenAsync(MakeConfig("TestWindow"), w => result = w);
            yield return null; // 等待协程一帧
            yield return null;
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TestWindow>(result);
        }

        [UnityTest]
        public IEnumerator OpenAsync_Cancel_BeforeLoad_CallbackWithNull()
        {
            UIWindow result = new TestWindow(); // 非 null 哨兵
            long uid = _manager.OpenAsync(MakeConfig("TestWindow"), w => result = w);
            _manager.Close(uid, withAnimation: false);
            yield return null;
            yield return null;
            Assert.IsNull(result);
        }

        // ===== Layer isolation =====

        [Test]
        public void Open_OnDifferentLayers_BothExist()
        {
            var c1 = MakeConfig("TestWindow");  c1.Layer = UILayer.Default;
            var c2 = MakeConfig("TestWindow2"); c2.Layer = UILayer.Popup;
            var w1 = _manager.Open(c1);
            var w2 = _manager.Open(c2);
            Assert.IsNotNull(w1);
            Assert.IsNotNull(w2);
            Assert.IsTrue(_manager.ExistUI(w1.UniqueId));
            Assert.IsTrue(_manager.ExistUI(w2.UniqueId));
        }

        // ===== SetResourceLoader =====

        [Test]
        public void SetResourceLoader_AfterInit_UsedOnNextOpen()
        {
            _manager.SetResourceLoader(null);
            var window = _manager.Open(MakeConfig("TestWindow"));
            Assert.IsNull(window); // no loader -> null

            _manager.SetResourceLoader(new TestResourceLoader());
            window = _manager.Open(MakeConfig("TestWindow"));
            Assert.IsNotNull(window);
        }

        // ===== Registry =====

        [Test]
        public void ScanAndRegisterAll_RegistersAttributeDecoratedWindow()
        {
            Assert.IsNotNull(_manager.GetRegistration_ForTest("TestWindowWithAttr"));
        }

        [Test]
        public void Open_UsingSimplifiedAPI_NoConfig_OpensWindow()
        {
            var window = _manager.Open<TestWindowWithAttr>();
            Assert.IsNotNull(window);
        }

        // ===== Query API =====

        [Test]
        public void GetWindow_AfterOpen_ReturnsInstance()
        {
            _manager.Open(MakeConfig("TestWindow"));
            var window = _manager.GetWindow<TestWindow>();
            Assert.IsNotNull(window);
        }

        [Test]
        public void TryGetWindow_NotOpen_ReturnsFalse()
        {
            Assert.IsFalse(_manager.TryGetWindow<TestWindow>(out _));
        }

        [Test]
        public void IsOpen_AfterOpen_ReturnsTrue()
        {
            _manager.Open(MakeConfig("TestWindow"));
            Assert.IsTrue(_manager.IsOpen<TestWindow>());
        }

        // ===== Message API =====

        [Test]
        public void SendMessage_ReachesWindow()
        {
            var window = _manager.Open(MakeConfig("TestWindow")) as TestWindow;
            _manager.SendMessage<TestWindow>("hello");
            Assert.AreEqual("hello", window.LastMsg);
        }
    }
}
