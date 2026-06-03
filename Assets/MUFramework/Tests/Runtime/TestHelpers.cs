using System.Collections;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    // 最小可用的 UIWindow 实现，用于测试
    public class TestWindow : UIWindow
    {
        public int OnCreateCount;
        public int OnOpenCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnPauseCount;
        public int OnResumeCount;
        public int OnCloseCount;
        public int OnDestroyCount;
        public object[] LastOpenArgs;
        public string LastMsg;

        protected override void OnCreate() => OnCreateCount++;
        protected override void OnOpen() => OnOpenCount++;
        internal override void OnOpenInternal(object[] args)
        {
            LastOpenArgs = args;
            base.OnOpenInternal(args);
        }
        protected override void OnShow() => OnShowCount++;
        protected override void OnHide() => OnHideCount++;
        protected override void OnPause() => OnPauseCount++;
        protected override void OnResume() => OnResumeCount++;
        protected override void OnClose() => OnCloseCount++;
        protected override void OnDestroy() => OnDestroyCount++;
        protected override void OnMessage(string msg, params object[] args) => LastMsg = msg;
    }

    public class TestWindow2 : UIWindow { }

    // 带 SkipCoveredCheck 属性的窗口
    public class OverlayWindow : UIWindow { }

    [UIWindowConfig(layer: UILayer.Default, cache: CacheType.None)]
    public class TestWindowWithAttr : UIWindow { }

    public class TestWidget : UIWidget
    {
        public int OpenCount;
        public int CloseCount;
        public int ShowCount;
        public int HideCount;
        public int PauseCount;
        public int ResumeCount;
        public int UpdateCount;

        protected override void OnOpen() => OpenCount++;
        protected override void OnClose() => CloseCount++;
        protected override void OnShow() => ShowCount++;
        protected override void OnHide() => HideCount++;
        protected override void OnPause() => PauseCount++;
        protected override void OnResume() => ResumeCount++;
        protected override void OnUpdate(float deltaTime) => UpdateCount++;
    }

    public class TestPanel : UIPanel
    {
        public int CreateCount;
        public int ActivateCount;
        public int DeactivateCount;
        public int DestroyCount;

        protected override void OnCreate() => CreateCount++;
        protected override void OnActivate() => ActivateCount++;
        protected override void OnDeactivate() => DeactivateCount++;
        protected override void OnDestroy() => DestroyCount++;
    }

    public class TestDataPanel : UIPanel<int>
    {
        public int LastData;
        protected override void OnActivate(int data) => LastData = data;
    }

    // 资源加载器：直接实例化 GameObject，无需实际资源
    public class TestResourceLoader : IUIResourceLoader
    {
        public GameObject LoadGameObject(string windowId)
        {
            var go = new GameObject(windowId);
            return go;
        }

        public IEnumerator LoadGameObjectAsync(string windowId, System.Action<GameObject> callback)
        {
            callback?.Invoke(LoadGameObject(windowId));
            yield return null;
        }
    }

    // 工厂方法映射 windowId -> Type，供 UIGlobal.GetWindowClassTypeFunc 使用
    public static class TestWindowFactory
    {
        public static System.Type GetType(string windowId)
        {
            return windowId switch
            {
                "TestWindow" => typeof(TestWindow),
                "TestWindow2" => typeof(TestWindow2),
                "OverlayWindow" => typeof(OverlayWindow),
                "TestWindowWithAttr" => typeof(TestWindowWithAttr),
                _ => null
            };
        }

        public static (bool, WindowOpenConfig) GetConfig(string windowId)
        {
            var config = WindowOpenConfig.Default;
            config.WindowId = windowId;
            return (true, config);
        }
    }

    // 记录日志，供断言使用
    public class TestLogger
    {
        public System.Collections.Generic.List<string> Errors = new();
        public System.Collections.Generic.List<string> Infos = new();

        public void Log(LogLevel level, string msg)
        {
            if (level == LogLevel.Error) Errors.Add(msg);
            else Infos.Add(msg);
        }
    }
}
