# MUFramework Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复所有已知核心 Bug，重构 UIWindow 基类体系（IOpenArgs、UIWindow\<TData\>、Pause/Resume 职责分离），统一枚举/方法拼写，为 Plan 2-4 提供稳定基础。

**Architecture:** 纯 C# 类修改，不涉及 Unity Editor。UIWindow 拆分为基类 + UIWindow\<TData\> 泛型子类；IOpenArgs\<T...\> 替换旧 IWindowOpenArgs；UIStackNode 成为唯一状态写入方；WindowOpenConfig 从 struct 改为 class。

**Tech Stack:** C# 9+、Unity 2021.3+、NUnit（EditMode 测试）

---

## 文件变更清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `Runtime/Core/Base/UIWindow.cs` | 重构生命周期、修复 Pause/Resume 双写、新增 OnOpenInternal/BindComponents/AutoBindComponents |
| 新增 | `Runtime/Core/Base/UIWindowGeneric.cs` | UIWindow\<TData\> 泛型基类 |
| 修改 | `Runtime/Core/Base/IWindowOpenArgsT.cs` | 重命名为 IOpenArgs，接口方法名改为 OnOpen |
| 修改 | `Runtime/Core/UIStackNode.cs` | 修复 Pause/Resume 双写、修正 AttackGameObject→AttachGameObject、修正 OnCovere→OnCovered |
| 修改 | `Runtime/Core/WindowOpenConfig.cs` | struct → class，字段 CoverdBehavior→CoveredBehavior |
| 修改 | `Runtime/Enums/CoverdBehavior.cs` | 重命名为 CoveredBehavior（文件 + 枚举名） |
| 修改 | `Runtime/Core/UIManager.cs` | 修复 Awake DontDestroyOnLoad 守卫、修复 RecycleUIStackNode 销毁 GO |
| 修改 | `Runtime/Core/UIStack.cs` | 修正 RecaculateSortingOrder→RecalculateSortingOrder，GetTopNode 加 skipBackKeyCheck |
| 修改 | `Runtime/Core/UIGlobal.cs` | UIEventHandler 签名升级 Action\<string\> → Action\<string,string\> |
| 修改 | `Runtime/AssemblyInfo.cs` | 已存在，确认 InternalsVisibleTo 正确 |
| 修改 | `Tests/Runtime/UIWindowTests.cs` | 更新测试以匹配新接口 |
| 修改 | `Tests/Runtime/UIManagerTests.cs` | 更新测试以匹配新接口 |
| 修改 | `Tests/Runtime/TestHelpers.cs` | 更新 TestWindow 以匹配新基类 |

---

## Task 1：重命名 CoverdBehavior → CoveredBehavior

**Files:**
- Modify: `Assets/MUFramework/Runtime/Enums/CoverdBehavior.cs`
- Modify: `Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIStack.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIStackNode.cs`

- [ ] **Step 1: 重命名枚举文件和枚举名**

将 `Assets/MUFramework/Runtime/Enums/CoverdBehavior.cs` 内容替换为：

```csharp
namespace MUFramework
{
    public enum CoveredBehavior
    {
        Normal,
        Pause,
        Hide,
    }
}
```

- [ ] **Step 2: 更新 WindowOpenConfig 字段类型**

在 `Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs` 中，将所有 `CoverdBehavior` 替换为 `CoveredBehavior`：

```csharp
/// <summary> 覆盖行为 </summary>
public CoveredBehavior WhenCovered;
```

- [ ] **Step 3: 更新 UIStack 中的引用**

在 `Assets/MUFramework/Runtime/Core/UIStack.cs` 中：
- 将 `(CoverdBehavior)coverLevel` → `(CoveredBehavior)coverLevel`
- 将 `CoverdBehavior finalCoverBehavior` → `CoveredBehavior finalCoverBehavior`
- 将 `case CoverdBehavior.Normal:` → `case CoveredBehavior.Normal:`
- 将 `case CoverdBehavior.Pause:` → `case CoveredBehavior.Pause:`
- 将 `case CoverdBehavior.Hide:` → `case CoveredBehavior.Hide:`
- 将方法名 `RecaculateSortingOrder` → `RecalculateSortingOrder`（方法定义和内部调用处都改）

- [ ] **Step 4: 更新 UIStackNode 拼写错误**

在 `Assets/MUFramework/Runtime/Core/UIStackNode.cs` 中：
- 将 `AttackGameObject` → `AttachGameObject`（方法名）
- 将 `Window.OnCovere()` → `Window.OnCovered()`
- 将 `Window.OnUncover()` → `Window.OnUncovered()`

- [ ] **Step 5: 编译验证**

打开 Unity Editor，等待编译完成，确认 Console 无红色报错。

- [ ] **Step 6: Commit**

```bash
git add Assets/MUFramework/Runtime/Enums/CoverdBehavior.cs
git add Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs
git add Assets/MUFramework/Runtime/Core/UIStack.cs
git add Assets/MUFramework/Runtime/Core/UIStackNode.cs
git commit -m "refactor: fix spelling errors CoverdBehavior→CoveredBehavior, AttackGameObject→AttachGameObject, OnCovere→OnCovered, RecaculateSortingOrder→RecalculateSortingOrder"
```

---

## Task 2：WindowOpenConfig struct → class

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs`

- [ ] **Step 1: 将 WindowOpenConfig 改为 class 并添加默认值**

将 `Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs` 全部替换为：

```csharp
using System.Collections.Generic;

namespace MUFramework
{
    public sealed class WindowOpenConfig
    {
        public string WindowId { get; set; }
        public UILayer Layer { get; set; } = UILayer.Normal;
        public CoveredBehavior WhenCovered { get; set; } = CoveredBehavior.Normal;
        public OpenBehavior OpenBehavior { get; set; } = OpenBehavior.KeepBelow;
        public CacheType CacheType { get; set; } = CacheType.None;
        public float ExpireTime { get; set; } = 60f;
        public bool AllowMultiInstance { get; set; } = false;
        public int MaxInstances { get; set; } = 1;
        public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.CloseOldest;
        public List<string> Dependencies { get; set; }
        public DependencyMissingPolicy DependencyMissingPolicy { get; set; }
        public WindowAttr WindowAttr { get; set; } = WindowAttr.None;
        public IUIAnimation UIAnimation { get; set; }

        public static WindowOpenConfig Default => new WindowOpenConfig();
    }
}
```

- [ ] **Step 2: 修复所有使用 WindowOpenConfig 的地方**

`UIStackNode.cs` 中 `OpenConfig` 属性类型自动兼容（已是引用类型），`UIManager.cs` 中传参方式不变。

检查 `Assets/MUFramework/Tests/Runtime/UIManagerTests.cs` 中的 `MakeConfig` 方法，将 struct 初始化语法改为对象初始化器（如已是对象初始化器则无需修改）：

```csharp
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
        Layer = UILayer.Normal,
    };
}
```

- [ ] **Step 3: 编译验证**

等待 Unity 编译，确认无报错。

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/WindowOpenConfig.cs
git add Assets/MUFramework/Tests/Runtime/UIManagerTests.cs
git commit -m "refactor: WindowOpenConfig struct→class with property defaults"
```

---

## Task 3：IOpenArgs 接口替换旧 IWindowOpenArgs

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/Base/IWindowOpenArgsT.cs`

- [ ] **Step 1: 替换接口定义**

将 `Assets/MUFramework/Runtime/Core/Base/IWindowOpenArgsT.cs` 全部替换为：

```csharp
namespace MUFramework
{
    /// <summary>
    /// 窗口打开参数接口（单参数）。
    /// Window 类继承此接口声明它接受哪种参数，框架通过泛型约束在编译期强制匹配。
    /// </summary>
    public interface IOpenArgs<T1>
    {
        void OnOpen(T1 arg1);
    }

    /// <summary> 窗口打开参数接口（双参数） </summary>
    public interface IOpenArgs<T1, T2>
    {
        void OnOpen(T1 arg1, T2 arg2);
    }

    /// <summary> 窗口打开参数接口（三参数） </summary>
    public interface IOpenArgs<T1, T2, T3>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3);
    }

    /// <summary> 窗口打开参数接口（四参数） </summary>
    public interface IOpenArgs<T1, T2, T3, T4>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    /// <summary> 窗口打开参数接口（五参数） </summary>
    public interface IOpenArgs<T1, T2, T3, T4, T5>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    // 旧接口保留别名，避免存量代码立即报错（后续 Plan 2 完成后移除）
    [System.Obsolete("Use IOpenArgs<T> instead of IWindowOpenArgs<T>")]
    public interface IWindowOpenArgs<T> : IOpenArgs<T> { }
    [System.Obsolete("Use IOpenArgs<T1,T2> instead")]
    public interface IWindowOpenArgs<T1, T2> : IOpenArgs<T1, T2> { }
    [System.Obsolete("Use IOpenArgs<T1,T2,T3> instead")]
    public interface IWindowOpenArgs<T1, T2, T3> : IOpenArgs<T1, T2, T3> { }
    [System.Obsolete("Use IOpenArgs<T1,T2,T3,T4> instead")]
    public interface IWindowOpenArgs<T1, T2, T3, T4> : IOpenArgs<T1, T2, T3, T4> { }
    [System.Obsolete("Use IOpenArgs<T1,T2,T3,T4> instead")]
    public interface IWindowOpenArgs<T1, T2, T3, T4, T5> : IOpenArgs<T1, T2, T3, T4> { }
}
```

- [ ] **Step 2: 编译验证**

等待 Unity 编译，确认无报错（旧接口通过 alias 兼容，会有 Obsolete warning，属正常）。

- [ ] **Step 3: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/Base/IWindowOpenArgsT.cs
git commit -m "refactor: introduce IOpenArgs<T> interfaces, deprecate IWindowOpenArgs<T>"
```

---

## Task 4：修复 UIWindow Pause/Resume 双写 + 重构生命周期

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/Base/UIWindow.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIStackNode.cs`

- [ ] **Step 1: 重构 UIWindow.cs**

将 `Assets/MUFramework/Runtime/Core/Base/UIWindow.cs` 全部替换为：

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MUFramework
{
    public abstract class UIWindow
    {
        public long UniqueId => _stackNode.UniqueId;
        public GameObject GameObject => _stackNode.GameObject;
        public Transform Transform => _stackNode.Transform;
        public CanvasGroup CanvasGroup => _stackNode.CanvasGroup;
        public Canvas Canvas => _stackNode.Canvas;
        public IUIAnimation UIAnimation => _stackNode.UIAnimation;
        public bool IsPause => _stackNode.IsPause;
        public bool IsCovered => _stackNode.IsCovered;
        public bool IsHidden => _stackNode.IsHidden;
        public bool IsClosing => _stackNode.IsClosing;
        public bool IsClosed => _stackNode.IsClosed;

        private readonly List<UIWidget> _widgets = new();
        private UIStackNode _stackNode;
        private bool _hasCreated = false;
        private long _animUniqueId;

        public void Init(UIStackNode stackNode)
        {
            _stackNode = stackNode;
            AutoBindComponents();
            BindComponents();
            if (_hasCreated) return;
            _hasCreated = true;
            OnCreate();
        }

        /// <summary> 框架内部调用：打开，传入参数（由 OpenCore 统一调用，勿重复调用） </summary>
        internal virtual void OnOpenInternal(object[] args) => OnOpen();

        /// <summary> 框架内部调用：消息分发 </summary>
        internal void OnMessageInternal(string msg, object[] args) => OnMessage(msg, args);

        /// <summary> 由 Editor Code Generator 生成覆写，勿手动修改 </summary>
        internal virtual void AutoBindComponents() { }

        /// <summary> 手动补充绑定，在 AutoBindComponents 之后执行 </summary>
        protected virtual void BindComponents() { }

        /// <summary> 框架内部：Pause/Resume 通知（由 UIStackNode 调用，勿直接调用） </summary>
        internal void OnPauseInternal(bool pause)
        {
            if (pause)
            {
                OnPause();
                for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyPause();
            }
            else
            {
                OnResume();
                for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyResume();
            }
        }

        /// <summary> 框架内部：Cover/Uncover 通知（由 UIStackNode 调用） </summary>
        internal void OnCoverInternal(bool covered)
        {
            if (covered) OnCovered();
            else OnUncovered();
        }

        public void Show(bool withAnimation = true, Action onComplete = null)
        {
            OnBeforeShow();
            SetInteractable(false);
            if (withAnimation && UIAnimation != null)
            {
                UIAnimation.Stop(_animUniqueId);
                _animUniqueId = UIAnimation.PlayOpen(GameObject, () =>
                {
                    SetInteractable(true);
                    GameObject.SetActive(true);
                    OnShow();
                    for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyShow();
                    onComplete?.Invoke();
                });
            }
            else
            {
                SetInteractable(true);
                GameObject.SetActive(true);
                OnShow();
                for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyShow();
                onComplete?.Invoke();
            }
        }

        public void Hide(bool withAnimation = true, Action onComplete = null)
        {
            OnBeforeHide();
            SetInteractable(false);
            if (withAnimation && UIAnimation != null)
            {
                UIAnimation.Stop(_animUniqueId);
                _animUniqueId = UIAnimation.PlayClose(GameObject, () =>
                {
                    GameObject.SetActive(false);
                    OnHide();
                    for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyHide();
                    onComplete?.Invoke();
                });
            }
            else
            {
                GameObject.SetActive(false);
                OnHide();
                for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyHide();
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 关闭窗口（外部调用入口，委托给 UIManager）
        /// </summary>
        public void Close(bool withAnimation = true, Action onComplete = null)
            => UIManager.Instance.Close(UniqueId, withAnimation, onComplete);

        /// <summary> 完成关闭：仅 UIManager 内部调用 </summary>
        internal void CompleteClose()
        {
            OnClose();
            for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyClose();
        }

        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
            for (int i = 0; i < _widgets.Count; i++) _widgets[i].Update(deltaTime);
        }

        public void UpdatePerSecond()
        {
            OnUpdatePerSecond();
        }

        public void Destroy()
        {
            OnDestroy();
            for (int i = 0; i < _widgets.Count; i++) _widgets[i].Destroy();
            _widgets.Clear();
        }

        public void SetInteractable(bool interactable)
        {
            if (CanvasGroup == null) return;
            CanvasGroup.interactable = interactable;
            CanvasGroup.blocksRaycasts = interactable;
        }

        public void AttachWidget(UIWidget widget)
        {
            if (_widgets.Contains(widget)) return;
            _widgets.Add(widget);
            widget.AttachTo(this);
        }

        // ===== 生命周期回调（子类重写）=====
        protected virtual void OnCreate() { }
        protected virtual void OnOpen() { }
        protected virtual void OnBeforeShow() { }
        protected virtual void OnShow() { }
        protected virtual void OnResume() { }
        protected virtual void OnCovered() { }
        protected virtual void OnUncovered() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnUpdatePerSecond() { }
        protected virtual void OnPause() { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnHide() { }
        protected virtual void OnClose() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnMessage(string msg, params object[] args) { }
    }
}
```

- [ ] **Step 2: 修复 UIStackNode 的 Pause/Resume/Cover 调用**

在 `Assets/MUFramework/Runtime/Core/UIStackNode.cs` 中修改 `SetPause`、`SetCover` 方法，改为通知而非直接调用 Window 方法：

```csharp
public void SetPause(bool pause)
{
    if (IsPause == pause) return;
    if (pause) SetState(UIState.Paused); else UnsetState(UIState.Paused);
    if (IsLoaded) Window?.OnPauseInternal(pause);
}

public void SetCover(bool cover)
{
    if (IsCovered == cover) return;
    if (cover) SetState(UIState.Covered); else UnsetState(UIState.Covered);
    if (IsLoaded) Window?.OnCoverInternal(cover);
}
```

同时将 `SetHide` 方法中的 `Window.Hide()` / `Window.Show()` 调用保持不变（Hide/Show 是外部可见方法，不是双写问题）。

- [ ] **Step 3: 编译验证**

等待 Unity 编译，确认无报错。

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/Base/UIWindow.cs
git add Assets/MUFramework/Runtime/Core/UIStackNode.cs
git commit -m "fix: resolve Pause/Resume double-write by routing state through UIStackNode; add AutoBindComponents/BindComponents hooks; fix OnCovered/OnUncovered visibility"
```

---

## Task 5：新增 UIWindow\<TData\> 泛型基类

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/Base/UIWindowGeneric.cs`
- Create: `Assets/MUFramework/Runtime/Core/Base/UIWindowGeneric.cs.meta`（Unity 自动生成）

- [ ] **Step 1: 创建 UIWindowGeneric.cs**

新建 `Assets/MUFramework/Runtime/Core/Base/UIWindowGeneric.cs`：

```csharp
namespace MUFramework
{
    /// <summary>
    /// 有参窗口基类（单一数据对象路径）。
    /// 等价于继承 UIWindow + IOpenArgs&lt;TData&gt;，适合参数是结构化数据对象的场景。
    /// 多参数或多种打开方式请直接继承 UIWindow + IOpenArgs&lt;T1, T2...&gt;。
    /// </summary>
    public abstract class UIWindow<TData> : UIWindow, IOpenArgs<TData>
    {
        /// <summary> 强类型打开回调，编译期强制实现 </summary>
        public abstract void OnOpen(TData data);

        internal override void OnOpenInternal(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is TData data)
                OnOpen(data);
            else
                OnOpen(default);
        }
    }
}
```

- [ ] **Step 2: 编译验证**

等待 Unity 编译，确认无报错。

- [ ] **Step 3: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/Base/UIWindowGeneric.cs
git commit -m "feat: add UIWindow<TData> generic base class for typed single-data-object open pattern"
```

---

## Task 6：修复 UIManager Bug（Awake 守卫 + RecycleUIStackNode）

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`

- [ ] **Step 1: 修复 Awake 中的 DontDestroyOnLoad**

在 `UIManager.cs` 的 `Awake` 方法中加 `Application.isPlaying` 守卫：

```csharp
private void Awake()
{
    if (_instance == null)
    {
        _instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);
    }
    else if (_instance != this)
    {
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: 修复 RecycleUIStackNode 不销毁 GameObject**

在 `UIManager.cs` 中找到 `RecycleUIStackNode` 方法，修改为：

```csharp
private void RecycleUIStackNode(UIStackNode node)
{
    if (node == null) return;
    var go = node.GameObject;
    node.Window?.Destroy();
    node.Dispose();
    if (go != null)
        UnityEngine.Object.Destroy(go);
    _uiStackNodePool.Push(node);
}
```

- [ ] **Step 3: 修复 Close 回调中重复的 SetActive(false)**

在 `UIManager.cs` 的 `Close(long uniqueId, ...)` 方法中，Hide 的回调里已经调用了 `GameObject.SetActive(false)`（在 `UIWindow.Hide` 内部），因此 Close 回调开头的 `node.GameObject.SetActive(false)` 是冗余的，删除它：

```csharp
node.Window.Hide(withAnimation, () =>
{
    // 不再重复调用 SetActive(false)，UIWindow.Hide 内部已处理
    node.SetClosedState();
    node.Window.CompleteClose();
    onComplete?.Invoke();
    switch (node.CacheType)
    {
        case CacheType.Persistent:
            AddUIToCache(node);
            break;
        case CacheType.ExpireTime:
            AddUIToCache(node);
            node.SetExpireTime(Time.unscaledTimeAsDouble + node.OpenConfig.ExpireTime);
            break;
        default:
            RemoveUIStackNode(node);
            break;
    }
});
```

- [ ] **Step 4: 修复 OpenCore 的 OnOpen 唯一调用点**

确认 `OpenCore` 内调用 `node.Window.OnOpenInternal(args)`，并检查 `OpenAsync` 强类型重载 callback 内是否有重复的 `ret.OnOpen(...)` 调用——如有，全部删除，callback 只负责回传引用：

```csharp
// OpenAsync 泛型重载 callback 示例（删除所有 ret.OnOpen 调用）
var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
{
    _asyncOpenCoroutines.Remove(uniqueId);
    if (window == null) { callback?.Invoke(null); return; }
    callback?.Invoke(window as TWindow);  // 只回传，不再调 OnOpen
}, args));
```

- [ ] **Step 5: 修复 AttackGameObject → AttachGameObject 调用处**

在 `UIManager.cs` 中将所有 `node.AttackGameObject(...)` 改为 `node.AttachGameObject(...)`。

- [ ] **Step 6: 编译验证**

等待 Unity 编译，确认无报错。

- [ ] **Step 7: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "fix: Awake DontDestroyOnLoad guard, RecycleUIStackNode destroys GameObject, remove duplicate SetActive and duplicate OnOpen call"
```

---

## Task 7：升级 UIGlobal.UIEventHandler 签名

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/UIGlobal.cs`

- [ ] **Step 1: 升级 UIEventHandler 签名**

在 `Assets/MUFramework/Runtime/Core/UIGlobal.cs` 中：

```csharp
// 旧：public static Action<string> UIEventHandler;
// 新：(eventName, windowId)
public static Action<string, string> UIEventHandler;
```

- [ ] **Step 2: 添加 FireUIEvent 辅助属性到 UIGlobal**

```csharp
public static void FireEvent(string eventName, string windowId)
    => UIEventHandler?.Invoke(eventName, windowId);
```

- [ ] **Step 3: 编译验证**

等待 Unity 编译，确认无报错（UIManager 内没有调用旧签名，如有则一并修改）。

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIGlobal.cs
git commit -m "feat: upgrade UIEventHandler signature to Action<string,string> (eventName, windowId)"
```

---

## Task 8：UIStack.GetTopNode 加 skipBackKeyCheck 参数

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/UIStack.cs`
- Modify: `Assets/MUFramework/Runtime/Enums/WindowAttr.cs`

- [ ] **Step 1: 在 WindowAttr 枚举中添加 SkipBackKey**

在 `Assets/MUFramework/Runtime/Enums/WindowAttr.cs` 中添加：

```csharp
[System.Flags]
public enum WindowAttr
{
    None            = 0,
    SkipCoveredCheck = 1 << 0,
    SkipBackKey     = 1 << 1,   // 新增：不响应返回键
}
```

- [ ] **Step 2: 修改 UIStack.GetTopNode 支持 skipBackKeyCheck**

在 `Assets/MUFramework/Runtime/Core/UIStack.cs` 中，将 `GetTopNode` 修改为：

```csharp
public UIStackNode GetTopNode(bool skipCoveredCheck = false, bool skipBackKeyCheck = false)
{
    for (int i = _stack.Count - 1; i >= 0; i--)
    {
        var node = _stack[i];
        if (node.IsClosing || node.IsClosed) continue;
        if (skipCoveredCheck && node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipCoveredCheck))
            continue;
        if (skipBackKeyCheck && node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipBackKey))
            continue;
        return node;
    }
    return null;
}
```

- [ ] **Step 3: 更新 UIManager.HandleBackKey 使用新参数**

在 `UIManager.cs` 的 `HandleBackKey` 方法中：

```csharp
public bool HandleBackKey()
{
    if (_uiLayers == null || _uiLayers.Length == 0) return false;
    for (int i = _uiLayers.Length - 1; i >= 0; i--)
    {
        var stack = _layerStacks[_uiLayers[i]];
        if (stack == null || stack.IsEmpty) continue;
        var topNode = stack.GetTopNode(skipBackKeyCheck: true);
        if (topNode != null && topNode.Window != null)
        {
            topNode.Window.Close();
            return true;
        }
    }
    return false;
}
```

- [ ] **Step 4: 编译验证**

- [ ] **Step 5: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIStack.cs
git add Assets/MUFramework/Runtime/Enums/WindowAttr.cs
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "feat: add SkipBackKey WindowAttr, update GetTopNode and HandleBackKey"
```

---

## Task 9：更新测试以匹配新接口

**Files:**
- Modify: `Assets/MUFramework/Tests/Runtime/TestHelpers.cs`
- Modify: `Assets/MUFramework/Tests/Runtime/UIWindowTests.cs`
- Modify: `Assets/MUFramework/Tests/Runtime/UIManagerTests.cs`

- [ ] **Step 1: 更新 TestHelpers.cs 中的 TestWindow**

在 `Assets/MUFramework/Tests/Runtime/TestHelpers.cs` 中，将 `TestWindow` 中所有旧拼写修正，并更新 `MakeConfig` 方法参数类型：

```csharp
// 确保 TestWindow 继承 UIWindow（无参）
// 将所有 CoverdBehavior → CoveredBehavior
// 将所有 IWindowOpenArgs → IOpenArgs
```

- [ ] **Step 2: 运行测试**

在 Unity Test Runner（EditMode）中运行 `MUIFramework.Tests`，观察失败项。

- [ ] **Step 3: 修复失败的测试**

针对报错逐一修正：
- 方法名拼写错误（`AttackGameObject` → `AttachGameObject`）
- 枚举名（`CoverdBehavior` → `CoveredBehavior`）
- `OnCovere` → `OnCovered`，`OnUncover` → `OnUncovered`

- [ ] **Step 4: 再次运行测试，确认全部通过**

在 Unity Test Runner 中运行，期望所有测试 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/MUFramework/Tests/
git commit -m "test: update tests to match renamed enums, methods, and new UIWindow interface"
```

---

## Task 10：UIStack.UpdateAllStackNode 使用 CoveredBehavior

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/UIStack.cs`

- [ ] **Step 1: 确认 UpdateAllStackNode 内引用已更新**

在 `UIStack.UpdateAllStackNode` 方法中，确认所有 `CoverdBehavior` 已改为 `CoveredBehavior`，`(int)node.OpenConfig.WhenCovered` 路径中的类型也已更新：

```csharp
private void UpdateAllStackNode()
{
    int coverLevel = 0;
    bool covered = false;
    for (int i = _stack.Count - 1; i >= 0; i--)
    {
        var node = _stack[i];
        if (node.IsClosing || node.IsClosed) continue;
        if (node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipCoveredCheck)) continue;

        node.SetCover(covered);
        CoveredBehavior finalBehavior = (int)node.OpenConfig.WhenCovered > coverLevel
            ? node.OpenConfig.WhenCovered
            : (CoveredBehavior)coverLevel;

        switch (finalBehavior)
        {
            case CoveredBehavior.Normal:
                node.SetHide(false);
                node.SetPause(false);
                break;
            case CoveredBehavior.Pause:
                node.SetHide(false);
                node.SetPause(true);
                break;
            case CoveredBehavior.Hide:
                node.SetPause(true);
                node.SetHide(true);
                break;
        }
        if ((int)node.OpenConfig.OpenBehavior > coverLevel)
            coverLevel = (int)node.OpenConfig.OpenBehavior;
        covered = true;
    }
}
```

- [ ] **Step 2: 编译并运行所有测试**

在 Unity Test Runner（EditMode）运行 `MUIFramework.Tests`，期望全部 PASS。

- [ ] **Step 3: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIStack.cs
git commit -m "refactor: update UIStack to use CoveredBehavior after rename"
```

---

## 自检记录

**Spec coverage:**
- ✅ Awake DontDestroyOnLoad 守卫（Task 6）
- ✅ Pause/Resume 双写（Task 4）
- ✅ OpenAsync OnOpen 双调（Task 6）
- ✅ RecycleUIStackNode 销毁 GO（Task 6）
- ✅ WindowOpenConfig struct→class（Task 2）
- ✅ 拼写错误统一修正（Task 1）
- ✅ UIEventHandler 签名升级（Task 7）
- ✅ IOpenArgs 替换 IWindowOpenArgs（Task 3）
- ✅ UIWindow\<TData\> 泛型基类（Task 5）
- ✅ SkipBackKey + HandleBackKey（Task 8）
- ✅ 测试更新（Task 9）

**未覆盖（Plan 2 处理）：**
- UIManager Partial 分拆
- Update 驱动 + 缓存过期清理
- Registry/Attribute 注册
- GetWindow\<T\>、SendMessage 新 API
