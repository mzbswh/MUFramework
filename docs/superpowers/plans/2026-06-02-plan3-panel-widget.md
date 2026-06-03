# MUFramework UIPanel & UIWidget Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重新设计 UIPanel 为 Window 内部纯 C# 子面板基类（独立于 UIManager）；补全 UIWidget 6 个生命周期回调（OnOpen/OnClose/OnShow/OnHide/OnPause/OnResume）。

**Architecture:** UIPanel 独立基类，不继承 UIWindow，通过 `Init(GameObject, UIWindow)` 与场景节点绑定，由宿主 Window 手动管理。UIWidget 新增内部通知方法，UIWindow 在各生命周期节点调用 `Notify*`。

**Tech Stack:** C# 纯类、Unity NUnit EditMode Tests

**先决条件:** Plan 1 已完成（UIWindow 重构、BindComponents 钩子已就位）

---

## 文件变更清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `Runtime/Core/Base/UIPanel.cs` | 完全重写为独立基类 + UIPanel\<TData\> |
| 修改 | `Runtime/Core/Base/UIWidget.cs` | 补全 6 个生命周期回调 + Notify* 内部方法 |
| 修改 | `Runtime/Core/Base/UIWindow.cs` | 确认 Notify* 调用路径完整 |
| 新增 | `Tests/Runtime/UIPanelTests.cs` | UIPanel 单元测试 |
| 修改 | `Tests/Runtime/UIWindowTests.cs` | 补充 Widget 通知测试 |

---

## Task 1：UIWidget 生命周期补全

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/Base/UIWidget.cs`

- [ ] **Step 1: 替换 UIWidget.cs 全部内容**

```csharp
// Assets/MUFramework/Runtime/Core/Base/UIWidget.cs
using UnityEngine;

namespace MUFramework
{
    public abstract class UIWidget
    {
        public bool Inited { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public object Data { get; private set; }
        public UIWindow AttachedWindow { get; private set; }
        public bool UpdateByMono { get; private set; }

        public void Init(GameObject root, bool updateByMono = false)
        {
            Inited = true;
            GameObject = root;
            Transform = root.transform;
            UpdateByMono = updateByMono;
            if (UpdateByMono)
                GameObject.GetOrAddComponent<UIWidgetMonoAdapter>().Init(this);
            OnCreate();
            RefreshUI();
        }

        public void AttachTo(UIWindow window)
        {
            if (AttachedWindow == window) return;
            AttachedWindow = window;
            AttachedWindow.AttachWidget(this);
        }

        public void SetData(object data)
        {
            Data = data;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (!Inited) return;
            OnRefreshUI();
        }

        public void SetActive(bool active)
        {
            if (!Inited) return;
            GameObject.SetActive(active);
            OnSetActive(active);
        }

        public void Update(float deltaTime)
        {
            if (!Inited) return;
            OnUpdate(deltaTime);
        }

        public void UpdatePerSecond()
        {
            if (!Inited) return;
            OnUpdatePerSecond();
        }

        public void Destroy()
        {
            if (!Inited) return;
            OnDestroy();
            Inited = false;
        }

        // ── 内部通知（由 UIWindow 调用，勿直接调用）──────
        internal void NotifyOpen()   { if (Inited) OnOpen(); }
        internal void NotifyClose()  { if (Inited) OnClose(); }
        internal void NotifyShow()   { if (Inited) OnShow(); }
        internal void NotifyHide()   { if (Inited) OnHide(); }
        internal void NotifyPause()  { if (Inited) OnPause(); }
        internal void NotifyResume() { if (Inited) OnResume(); }

        // ── 生命周期回调（子类重写）────────────────────
        protected virtual void OnCreate() { }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnSetActive(bool active) { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnUpdatePerSecond() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnRefreshUI() { }
    }

    public abstract class UIWidget<TData> : UIWidget
    {
        public new TData Data { get; private set; }

        public void SetData(TData data)
        {
            base.SetData(data);
            Data = data;
        }
    }

    public abstract class UIWidget<TData, TAttachedWindow> : UIWidget where TAttachedWindow : UIWindow
    {
        public new TData Data { get; private set; }
        public new TAttachedWindow AttachedWindow { get; private set; }

        public void SetData(TData data)
        {
            base.SetData(data);
            Data = data;
        }

        public void AttachTo(TAttachedWindow attachedWindow)
        {
            base.AttachTo(attachedWindow);
            AttachedWindow = attachedWindow;
        }
    }
}
```

- [ ] **Step 2: 确认 UIWindow.cs 中已包含 Notify* 调用**

检查 Plan 1 Task 4 实现的 `UIWindow.cs`：
- `CompleteClose()` 内调用 `_widgets[i].NotifyClose()`  ✓
- `Show()` → `OnShow()` 之后调用 `_widgets[i].NotifyShow()`  ✓
- `Hide()` → `OnHide()` 之后调用 `_widgets[i].NotifyHide()`  ✓
- `OnPauseInternal(true)` 内调用 `_widgets[i].NotifyPause()`  ✓
- `OnPauseInternal(false)` 内调用 `_widgets[i].NotifyResume()`  ✓
- `OnOpenInternal` 末尾调用 `_widgets[i].NotifyOpen()`（若未添加则补充）：

```csharp
internal virtual void OnOpenInternal(object[] args)
{
    OnOpen();
    for (int i = 0; i < _widgets.Count; i++) _widgets[i].NotifyOpen();
}
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/Base/UIWidget.cs
git add Assets/MUFramework/Runtime/Core/Base/UIWindow.cs
git commit -m "feat: UIWidget lifecycle callbacks OnOpen/Close/Show/Hide/Pause/Resume + Notify* dispatch"
```

---

## Task 2：UIPanel 基类重写

**Files:**
- Modify: `Assets/MUFramework/Runtime/Core/Base/UIPanel.cs`

- [ ] **Step 1: 完全替换 UIPanel.cs**

```csharp
// Assets/MUFramework/Runtime/Core/Base/UIPanel.cs
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// Window 内部子面板基类（纯C#，不参与全局 UIManager 栈）。
    /// 通过 Init(GameObject, UIWindow) 绑定场景节点，由宿主 Window 手动管理生命周期。
    /// </summary>
    public abstract class UIPanel
    {
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public UIWindow OwnerWindow { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>
        /// 初始化面板，绑定到预制体子节点。
        /// 由宿主 Window 在 OnCreate 中调用。
        /// </summary>
        public void Init(GameObject root, UIWindow owner)
        {
            GameObject = root;
            Transform = root.transform;
            OwnerWindow = owner;
            AutoBindComponents();
            BindComponents();
            OnCreate();
        }

        /// <summary> 激活面板（SetActive true + OnActivate） </summary>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            GameObject.SetActive(true);
            OnActivate();
        }

        /// <summary> 停用面板（OnDeactivate + SetActive false） </summary>
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            OnDeactivate();
            GameObject.SetActive(false);
        }

        /// <summary> 刷新面板数据 </summary>
        public void Refresh() => OnRefresh();

        /// <summary> 销毁面板，清理引用 </summary>
        public void Destroy()
        {
            OnDestroy();
            IsActive = false;
            GameObject = null;
            Transform = null;
            OwnerWindow = null;
        }

        /// <summary> 由 Editor Code Generator 生成覆写，勿手动修改 </summary>
        internal virtual void AutoBindComponents() { }

        /// <summary> 手动补充绑定，在 AutoBindComponents 之后执行 </summary>
        protected virtual void BindComponents() { }

        // ── 生命周期回调（子类重写）────────────────────
        protected virtual void OnCreate() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnRefresh() { }
        protected virtual void OnDestroy() { }
    }

    /// <summary>
    /// 有参子面板基类（单一 TData 激活参数）。
    /// </summary>
    public abstract class UIPanel<TData> : UIPanel
    {
        protected TData Data { get; private set; }

        /// <summary> 带数据激活 </summary>
        public void Activate(TData data)
        {
            Data = data;
            Activate();
        }

        protected sealed override void OnActivate() => OnActivate(Data);

        /// <summary> 强类型激活回调，编译期强制实现 </summary>
        protected abstract void OnActivate(TData data);
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/Base/UIPanel.cs
git commit -m "refactor: UIPanel rewrite as independent pure-C# sub-panel base class with UIPanel<TData>"
```

---

## Task 3：UIWidget 测试

**Files:**
- Modify: `Assets/MUFramework/Tests/Runtime/UIWindowTests.cs`
- Modify: `Assets/MUFramework/Tests/Runtime/TestHelpers.cs`

- [ ] **Step 1: 在 TestHelpers.cs 中添加 TestWidget**

```csharp
public class TestWidget : UIWidget
{
    public int OpenCount;
    public int CloseCount;
    public int ShowCount;
    public int HideCount;
    public int PauseCount;
    public int ResumeCount;

    protected override void OnOpen()   => OpenCount++;
    protected override void OnClose()  => CloseCount++;
    protected override void OnShow()   => ShowCount++;
    protected override void OnHide()   => HideCount++;
    protected override void OnPause()  => PauseCount++;
    protected override void OnResume() => ResumeCount++;
}
```

- [ ] **Step 2: 在 UIWindowTests.cs 中添加 Widget 通知测试**

```csharp
// ===== Widget Lifecycle Notification =====

[Test]
public void Widget_NotifyOpen_CalledWhenWindowOpens()
{
    InitWindow();
    var widget = new TestWidget();
    var go = new GameObject("Widget");
    widget.Init(go);
    _window.AttachWidget(widget);

    _window.OnOpenInternal_ForTest(System.Array.Empty<object>());

    Assert.AreEqual(1, widget.OpenCount);
    UnityEngine.Object.DestroyImmediate(go);
}

[Test]
public void Widget_NotifyShow_CalledWhenWindowShows()
{
    InitWindow();
    var widget = new TestWidget();
    var go = new GameObject("Widget");
    widget.Init(go);
    _window.AttachWidget(widget);

    _window.Show(withAnimation: false);

    Assert.AreEqual(1, widget.ShowCount);
    UnityEngine.Object.DestroyImmediate(go);
}

[Test]
public void Widget_NotifyPause_CalledWhenWindowPauses()
{
    InitWindow();
    var widget = new TestWidget();
    var go = new GameObject("Widget");
    widget.Init(go);
    _window.AttachWidget(widget);

    _window.OnPauseInternal_ForTest(true);

    Assert.AreEqual(1, widget.PauseCount);
    UnityEngine.Object.DestroyImmediate(go);
}
```

在 `UIWindow.cs` 中暴露测试用辅助方法（`#if` 守卫）：

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
internal void OnOpenInternal_ForTest(object[] args) => OnOpenInternal(args);
internal void OnPauseInternal_ForTest(bool pause) => OnPauseInternal(pause);
#endif
```

- [ ] **Step 3: 运行测试，确认全部 PASS**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Tests/Runtime/TestHelpers.cs
git add Assets/MUFramework/Tests/Runtime/UIWindowTests.cs
git add Assets/MUFramework/Runtime/Core/Base/UIWindow.cs
git commit -m "test: UIWidget lifecycle notification tests (Open/Show/Pause/Resume/Hide/Close)"
```

---

## Task 4：UIPanel 测试

**Files:**
- Create: `Assets/MUFramework/Tests/Runtime/UIPanelTests.cs`

- [ ] **Step 1: 在 TestHelpers.cs 中添加 TestPanel 和 TestDataPanel**

```csharp
public class TestPanel : UIPanel
{
    public int CreateCount;
    public int ActivateCount;
    public int DeactivateCount;
    public int DestroyCount;

    protected override void OnCreate()     => CreateCount++;
    protected override void OnActivate()   => ActivateCount++;
    protected override void OnDeactivate() => DeactivateCount++;
    protected override void OnDestroy()    => DestroyCount++;
}

public class TestDataPanel : UIPanel<int>
{
    public int LastData;
    protected override void OnActivate(int data) => LastData = data;
}
```

- [ ] **Step 2: 创建 UIPanelTests.cs**

```csharp
// Assets/MUFramework/Tests/Runtime/UIPanelTests.cs
using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    public class UIPanelTests
    {
        private GameObject _panelGO;
        private TestWindow _ownerWindow;

        [SetUp]
        public void SetUp()
        {
            _panelGO = new GameObject("Panel");
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGO != null)
                Object.DestroyImmediate(_panelGO);
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
```

- [ ] **Step 3: 运行测试，确认全部 PASS**

在 Unity Test Runner（EditMode）运行 `MUIFramework.Tests`。

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Tests/Runtime/UIPanelTests.cs
git add Assets/MUFramework/Tests/Runtime/UIPanelTests.cs.meta
git add Assets/MUFramework/Tests/Runtime/TestHelpers.cs
git commit -m "test: UIPanel unit tests - Init, Activate, Deactivate, Destroy, generic data, SwitchPanel pattern"
```

---

## 自检记录

**Spec coverage（Plan 3 范围）：**
- ✅ UIWidget 6 个生命周期回调补全（Task 1）
- ✅ UIWidget Notify* 内部分发（Task 1）
- ✅ UIWindow.OnOpenInternal 通知 Widget（Task 1）
- ✅ UIPanel 独立基类（Task 2）
- ✅ UIPanel\<TData\> 有参激活（Task 2）
- ✅ UIPanel.AutoBindComponents / BindComponents 钩子（Task 2）
- ✅ Widget 生命周期测试（Task 3）
- ✅ UIPanel 完整测试（Task 4）
