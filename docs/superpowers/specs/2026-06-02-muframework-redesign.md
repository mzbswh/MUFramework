# MUFramework 重设计规格文档

> 日期：2026-06-02
> 范围：全量重设计（接口层 + Bug修复 + 扩展层 + SG Binding 默认内置）

---

## 一、背景与目标

MUFramework 是基于 Unity UGUI 的轻量独立 UI 管理框架，定位为**通用框架库**。
本次重设计目标：

1. 提供分层 API：简单用法一行，复杂用法完全可控
2. 参数传递编译期类型安全，消除 `object[]` 不确定性
3. 修复已知核心 Bug（Pause双写、OnOpen双调、Awake守卫等）
4. 补全缺失功能（Update驱动、缓存清理、UIEventHandler、HandleMsg）
5. 异步双轨：默认 Coroutine+callback，可选 UniTask awaitable
6. UIPanel 重新定位为 Window 内部子面板（纯C#类）
7. UIWidget 补全生命周期对齐
8. SG Binding **默认内置**：组件绑定自动生成 + `[OpenArgs]` 多参数打开声明

对比参考：GameFramework（事件体系、RecycleQueue）、YIUI（代码生成、强类型参数）。

---

## 二、架构总览

### 分层结构

```
┌─────────────────────────────────────────────────┐
│              简化 API 层（Simple API）            │
│  Open<ShopWindow>()  /  Open<ShopWindow, TData>() │
│  自动查 Attribute 注册表，零配置                   │
├─────────────────────────────────────────────────┤
│              完整 API 层（Full API）              │
│  Open(WindowOpenConfig, data)                    │
│  动态配置、热更新、脚本驱动场景                    │
├─────────────────────────────────────────────────┤
│              核心层（Core）                       │
│  UIManager / UIStack / UIStackNode               │
│  状态机、层级、覆盖逻辑、缓存、Update驱动         │
├─────────────────────────────────────────────────┤
│              扩展层（Extensions）                 │
│  UniTask 适配（可选）/ SG Binding（默认内置）      │
└─────────────────────────────────────────────────┘
```

### 核心数据结构

```
UIManager
├── _layerStacks: Dictionary<UILayer, UIStack>
├── _windowInstances: Dictionary<string, List<UIStackNode>>
├── _allWindows: Dictionary<long, UIStackNode>
├── _cachedUI: Dictionary<string, List<UIStackNode>>   // 改为 List（支持过期遍历删除）
├── _nodePool: Stack<UIStackNode>
├── _registry: Dictionary<string, WindowRegistration>  // 新增：注册表
├── _updateSnapshot: List<UIStackNode>                 // Update 快照，避免遍历中修改
└── _asyncCoroutines: Dictionary<long, Coroutine>
```

### UIManager Partial 分拆

| 文件 | 职责 |
|------|------|
| `UIManager.cs` | 单例、Awake、Initialize、AddUIStackNode/RemoveUIStackNode 共享方法 |
| `UIManager.Open.cs` | Open、OpenAsync、OpenCore、OpenAsyncCoroutine |
| `UIManager.Close.cs` | Close、CloseAll、CancelAsync、关闭回调流程 |
| `UIManager.Query.cs` | GetWindow、TryGetWindow、IsOpen、ExistUI |
| `UIManager.Message.cs` | SendMessage、HandleBackKey |
| `UIManager.Registry.cs` | ScanAndRegisterAll、Register、WindowId 推导 |
| `UIManager.Cache.cs` | AddUIToCache、TryGetCachedUI、CleanExpiredCache |
| `UIManager.Update.cs` | Update 驱动循环、UpdatePerSecond 计时、过期检查触发 |
| `UIManager.NodePool.cs` | CreateUIStackNode、RecycleUIStackNode、GetNewUIStackNode |

---

## 三、Window 基类与参数传递

### 继承体系

```csharp
// 无参窗口基类
public abstract class UIWindow
{
    // 生命周期（protected，子类重写）
    protected virtual void OnCreate() { }
    protected virtual void OnOpen() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnResume() { }
    protected virtual void OnPause() { }
    protected virtual void OnClose() { }
    protected virtual void OnDestroy() { }
    protected virtual void OnCovered() { }        // 拼写修正（原 OnCovere）
    protected virtual void OnUncovered() { }      // 拼写修正（原 OnUncover），改为 protected

    // 消息接收
    protected virtual void OnMessage(string msg, params object[] args) { }

    // 框架内部桥接（子类不可见）
    internal virtual void OnOpenInternal(object[] args) => OnOpen();
    internal virtual void OnMessageInternal(string msg, object[] args) => OnMessage(msg, args);
    internal virtual void BindComponents() { }    // SG 默认生成覆写此钩子
}

// UIPanel（Window 内子面板，独立基类，不继承 UIWindow，不参与全局栈）
// 完整设计见 Section 八
```

### 参数传递：`IOpenArgs<T...>` 接口 + SG 辅助生成

**设计原则**：Window 类直接继承 `IOpenArgs<T...>` 接口声明参数契约，SG 检测已有接口继承后只负责生成动态分发 `OnOpenInternal` 和组件绑定，**不生成任何 abstract 方法，不需要 `[OpenArgs]` Attribute**。开发者直接实现接口方法，UIManager 通过泛型约束编译期区分调用路径。

#### 接口定义（框架手写，固定数量）

```csharp
// Runtime/Core/Interfaces/IOpenArgs.cs
public interface IOpenArgs<T1>
{
    void OnOpen(T1 arg1);  // 开发者直接实现，不是 abstract
}
public interface IOpenArgs<T1, T2>
{
    void OnOpen(T1 arg1, T2 arg2);
}
public interface IOpenArgs<T1, T2, T3>
{
    void OnOpen(T1 arg1, T2 arg2, T3 arg3);
}
// 最多支持到 T4，覆盖 99% 场景
```

#### 开发者手写（无任何额外 Attribute）

```csharp
[UIWindowConfig(layer: UILayer.Normal)]  // 只保留配置注册 Attribute
public partial class ShopWindow : UIWindow, IOpenArgs<int>, IOpenArgs<string, bool>
{
    private Button _closeButton;  // SG 自动绑定

    // 直接实现接口方法，不是 abstract，漏掉即编译报错（接口未实现）
    public void OnOpen(int tab) { ... }
    public void OnOpen(string itemId, bool fromBattle) { ... }

    protected override void OnCreate()
    {
        _closeButton.onClick.AddListener(OnCloseClick);
    }
}
```

#### SG 生成（ShopWindow.Generated.cs，只读）

SG 检测到 `IOpenArgs<int>` 和 `IOpenArgs<string, bool>`，生成两件事：

```csharp
public partial class ShopWindow
{
    // 1. 动态分发（完整 API / 热更路径，object[] 入口）
    internal override void OnOpenInternal(object[] args)
    {
        if (args.Length == 1 && args[0] is int t)
            { ((IOpenArgs<int>)this).OnOpen(t); return; }
        if (args.Length == 2 && args[0] is string id && args[1] is bool f)
            { ((IOpenArgs<string, bool>)this).OnOpen(id, f); return; }
        base.OnOpenInternal(args);  // 兜底无参 OnOpen()
    }

    // 2. 组件绑定
    internal override void BindComponents()
    {
        _closeButton = Transform.Find("CloseButton")?.GetComponent<Button>();
    }
}
```

#### UIManager 固定重载（手写，不需要 SG）

```csharp
// UIManager.Open.cs — 通过泛型约束编译期区分，固定写死
public TWindow Open<TWindow, T1>(T1 arg1)
    where TWindow : UIWindow, IOpenArgs<T1>;

public TWindow Open<TWindow, T1, T2>(T1 arg1, T2 arg2)
    where TWindow : UIWindow, IOpenArgs<T1, T2>;

public TWindow Open<TWindow, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
    where TWindow : UIWindow, IOpenArgs<T1, T2, T3>;

public long OpenAsync<TWindow, T1>(T1 arg1, Action<TWindow> callback)
    where TWindow : UIWindow, IOpenArgs<T1>;

public long OpenAsync<TWindow, T1, T2>(T1 arg1, T2 arg2, Action<TWindow> callback)
    where TWindow : UIWindow, IOpenArgs<T1, T2>;
```

#### 调用方体验

```csharp
manager.Open<ShopWindow, int>(1);                      // → IOpenArgs<int>.OnOpen(1)        ✅ 编译期
manager.Open<ShopWindow, string, bool>("sword", true); // → IOpenArgs<string,bool>.OnOpen() ✅ 编译期
manager.Open<ShopWindow, float>(1.0f);                 // ❌ 编译报错：ShopWindow 未实现 IOpenArgs<float>
manager.Open<ShopWindow>();                            // → OnOpen()                        ✅ 无参路径
```

#### 与 `UIWindow<TData>` 泛型基类的关系

`UIWindow<TData>` 是 `IOpenArgs<TData>` 的**快捷路径**，适合单一结构化数据对象场景，内部已实现接口：

```csharp
// UIWindow<TData> 等价于手动继承 IOpenArgs<TData>，选其一，不混用
public abstract class UIWindow<TData> : UIWindow, IOpenArgs<TData>
{
    public abstract void OnOpen(TData data);
    internal override void OnOpenInternal(object[] args)
        => OnOpen((TData)args[0]);
}

// 单一数据对象 → 用泛型基类（更简洁）
public class ShopWindow : UIWindow<ShopOpenData>
{
    public override void OnOpen(ShopOpenData data) { ... }
}

// 多参数 / 多种打开方式 → 直接继承 IOpenArgs<T...>（更灵活）
public partial class ShopWindow : UIWindow, IOpenArgs<int>, IOpenArgs<string, bool>
{
    public void OnOpen(int tab) { ... }
    public void OnOpen(string itemId, bool fromBattle) { ... }
}
```

### 职责边界：消除 Pause/Resume 双写

**原则**：状态只由 `UIStackNode` 持有和写入，`UIWindow` 只提供回调钩子，不直接写 stackNode 状态。

```csharp
// UIStackNode 是唯一的状态写入方
public void SetPause(bool pause)
{
    if (IsPause == pause) return;
    if (pause) SetState(UIState.Paused); else UnsetState(UIState.Paused);
    Window?.OnPauseInternal(pause);
}

// UIWindow 内部，只做回调派发
internal void OnPauseInternal(bool pause)
{
    if (pause) OnPause(); else OnResume();
    foreach (var w in _widgets)
    {
        if (pause) w.NotifyPause(); else w.NotifyResume();
    }
}

// UIWindow 对外的 Pause/Resume 委托给 UIManager，不再直接写 stackNode
public void Pause() => UIManager.Instance.PauseWindow(UniqueId);
public void Resume() => UIManager.Instance.ResumeWindow(UniqueId);
```

### OpenCore 是唯一 OnOpen 调用点（修复双调 Bug）

```csharp
private UIWindow OpenCore(UIStackNode node, object data)
{
    node.Window.Init(node);           // BindComponents() 在 Init 内调用
    node.Window.OnOpenInternal(data); // 唯一调用点
    node.Window.Show();
    SafeAreaAdapter.AdaptSafeArea(node.GameObject);
    FireUIEvent(UIGlobal.UI_EVENT_WINDOW_OPEN, node.WindowId);
    UpdateAllStacks();
    return node.Window;
}
// OpenAsync callback 只回传引用，不再重复调用 OnOpen
```

---

## 四、配置系统（两级 API）

### WindowOpenConfig 改为 class

```csharp
public sealed class WindowOpenConfig
{
    public string WindowId { get; set; }
    public UILayer Layer { get; set; } = UILayer.Normal;
    public CacheType CacheType { get; set; } = CacheType.None;
    public float ExpireTime { get; set; } = 60f;
    public OpenBehavior OpenBehavior { get; set; } = OpenBehavior.KeepBelow;
    public CoveredBehavior WhenCovered { get; set; } = CoveredBehavior.Normal;  // 拼写修正
    public bool AllowMultiInstance { get; set; } = false;
    public int MaxInstances { get; set; } = 1;
    public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.CloseOldest;
    public List<string> Dependencies { get; set; }
    public DependencyMissingPolicy DependencyMissingPolicy { get; set; }
    public WindowAttr WindowAttr { get; set; } = WindowAttr.None;
    public IUIAnimation UIAnimation { get; set; }
}
```

### Attribute 注册（简化 API 入口）

```csharp
[UIWindowConfig(
    layer: UILayer.Normal,
    cache: CacheType.Persistent,
    openBehavior: OpenBehavior.HideBelow
)]
public class ShopWindow : UIWindow<ShopOpenData> { ... }
```

框架 `Initialize()` 内自动调用 `ScanAndRegisterAll()`，反射扫描所有带 `[UIWindowConfig]` 的类型，收集到 `_registry`。

### WindowId 推导规则

- 默认：类名即 WindowId（`ShopWindow` → `"ShopWindow"`）
- Attribute 可覆写：`[UIWindowConfig(windowId: "Shop_V2", ...)]`

### 注册表结构

```csharp
public sealed class WindowRegistration
{
    public string WindowId { get; }
    public Type WindowType { get; }
    public WindowOpenConfig DefaultConfig { get; }
}
```

### 两级 API 调用对比

```csharp
// ── 简化 API（Attribute 驱动，零配置）──
manager.Open<ShopWindow>();
manager.Open<ShopWindow, ShopOpenData>(new ShopOpenData(Tab: 1));

// ── 混合（简化 API + 局部覆写）──
manager.Open<ShopWindow, ShopOpenData>(
    data: new ShopOpenData(Tab: 1),
    configOverride: cfg => cfg.Layer = UILayer.Top
);

// ── 完整 API（显式 config，动态/热更场景）──
var config = new WindowOpenConfig { WindowId = "ShopWindow", Layer = UILayer.Normal };
manager.Open(config, new ShopOpenData(Tab: 1));
```

---

## 五、UIManager 核心接口

### Open / Close / Get 完整签名

```csharp
public partial class UIManager : MonoBehaviour
{
    // ── 简化 API：无参 ────────────────────────────────
    public TWindow Open<TWindow>()
        where TWindow : UIWindow;

    // ── 简化 API：单数据对象（UIWindow<TData> 路径）──
    public TWindow Open<TWindow, TData>(TData data)
        where TWindow : UIWindow<TData>;
    public TWindow Open<TWindow, TData>(TData data, Action<WindowOpenConfig> configOverride)
        where TWindow : UIWindow<TData>;

    // ── 简化 API：[OpenArgs] 多参数路径（IOpenArgs 约束）──
    public TWindow Open<TWindow, T1>(T1 arg1)
        where TWindow : UIWindow, IOpenArgs<T1>;
    public TWindow Open<TWindow, T1, T2>(T1 arg1, T2 arg2)
        where TWindow : UIWindow, IOpenArgs<T1, T2>;
    public TWindow Open<TWindow, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        where TWindow : UIWindow, IOpenArgs<T1, T2, T3>;

    // ── 异步：无参 ────────────────────────────────────
    public long OpenAsync<TWindow>(Action<TWindow> callback)
        where TWindow : UIWindow;

    // ── 异步：单数据对象 ──────────────────────────────
    public long OpenAsync<TWindow, TData>(TData data, Action<TWindow> callback)
        where TWindow : UIWindow<TData>;

    // ── 异步：[OpenArgs] 多参数路径 ───────────────────
    public long OpenAsync<TWindow, T1>(T1 arg1, Action<TWindow> callback)
        where TWindow : UIWindow, IOpenArgs<T1>;
    public long OpenAsync<TWindow, T1, T2>(T1 arg1, T2 arg2, Action<TWindow> callback)
        where TWindow : UIWindow, IOpenArgs<T1, T2>;

    // ── 完整 API ──────────────────────────────────────
    public UIWindow Open(WindowOpenConfig config, object data = null);
    public long OpenAsync(WindowOpenConfig config, Action<UIWindow> callback, object data = null);

    // ── Close ─────────────────────────────────────────
    public void Close<TWindow>(bool withAnimation = true)
        where TWindow : UIWindow;
    public void Close(long uniqueId, bool withAnimation = true, Action onComplete = null);
    public void CloseAll<TWindow>(bool withAnimation = true)
        where TWindow : UIWindow;
    public void CloseAll(bool withAnimation = true);
    public bool CancelAsync(long uniqueId);

    // ── Query ─────────────────────────────────────────
    public TWindow GetWindow<TWindow>()
        where TWindow : UIWindow;
    public bool TryGetWindow<TWindow>(out TWindow window)
        where TWindow : UIWindow;
    public IReadOnlyList<TWindow> GetWindows<TWindow>()
        where TWindow : UIWindow;
    public bool IsOpen<TWindow>()
        where TWindow : UIWindow;

    // ── Message ───────────────────────────────────────
    public void SendMessage<TWindow>(string msg, params object[] args)
        where TWindow : UIWindow;
    public void SendMessage(long uniqueId, string msg, params object[] args);
    public bool HandleBackKey();

    // ── Setup ─────────────────────────────────────────
    public void SetResourceLoader(IUIResourceLoader loader);
    public void Initialize();
    public void Register(WindowRegistration registration);
    public void ScanAndRegisterAll();
}
```

### UIEventHandler 升级

```csharp
// UIGlobal.cs
public static Action<string, string> UIEventHandler;  // (eventName, windowId)

// 触发时机
// UI_EVENT_WINDOW_OPEN    → OpenCore 成功后
// UI_EVENT_WINDOW_CLOSE   → Close 回调完成后
// UI_EVENT_WINDOW_SHOW    → Show 动画结束后
// UI_EVENT_WINDOW_HIDE    → Hide 动画结束后
// UI_EVENT_WINDOW_PAUSE   → SetPause(true)
// UI_EVENT_WINDOW_RESUME  → SetPause(false)
// UI_EVENT_WINDOW_DESTROY → Destroy 调用后
```

---

## 六、Update 驱动与缓存清理

### Update 驱动（UIManager.Update.cs）

```csharp
private readonly List<UIStackNode> _updateSnapshot = new();
private float _perSecondTimer;
private float _cacheCleanTimer;
private const float PER_SECOND_INTERVAL = 1f;
private const float CACHE_CLEAN_INTERVAL = 5f;

private void Update()
{
    float dt = Time.unscaledDeltaTime;

    // 快照防止遍历中集合变更
    _updateSnapshot.Clear();
    foreach (var node in _allWindows.Values) _updateSnapshot.Add(node);

    foreach (var node in _updateSnapshot)
    {
        if (node.IsLoaded && !node.IsPause && !node.IsClosing)
            node.Window.Update(dt);
    }

    _perSecondTimer += dt;
    if (_perSecondTimer >= PER_SECOND_INTERVAL)
    {
        _perSecondTimer = 0f;
        foreach (var node in _updateSnapshot)
        {
            if (node.IsLoaded && !node.IsPause && !node.IsClosing)
                node.Window.UpdatePerSecond();
        }
    }

    _cacheCleanTimer += dt;
    if (_cacheCleanTimer >= CACHE_CLEAN_INTERVAL)
    {
        _cacheCleanTimer = 0f;
        CleanExpiredCache();
    }
}
```

### 缓存过期清理（UIManager.Cache.cs）

缓存容器从 `Stack<UIStackNode>` 改为 `List<UIStackNode>`：
- `Persistent`：取末尾元素（等价 Stack.Pop）
- `ExpireTime`：按条件遍历删除（Stack 做不到）

```csharp
private void CleanExpiredCache()
{
    double now = Time.unscaledTimeAsDouble;
    foreach (var (windowId, list) in _cachedUI)
    {
        list.RemoveAll(node =>
        {
            if (node.ExpireTime > 0 && now >= node.ExpireTime)
            {
                RecycleUIStackNode(node);
                return true;
            }
            return false;
        });
    }
    _stringListCache.Clear();
    foreach (var key in _cachedUI.Keys)
        if (_cachedUI[key].Count == 0) _stringListCache.Add(key);
    foreach (var key in _stringListCache) _cachedUI.Remove(key);
}
```

---

## 七、UIWidget 生命周期对齐

```csharp
public abstract class UIWidget
{
    // 现有生命周期（保留）
    protected virtual void OnCreate() { }
    protected virtual void OnUpdate(float deltaTime) { }
    protected virtual void OnUpdatePerSecond() { }
    protected virtual void OnDestroy() { }
    protected virtual void OnRefreshUI() { }
    protected virtual void OnSetActive(bool active) { }

    // 新增：与宿主 Window 生命周期对齐
    protected virtual void OnOpen() { }
    protected virtual void OnClose() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnPause() { }
    protected virtual void OnResume() { }

    // 框架内部通知（UIWindow 调用）
    internal void NotifyOpen()   { if (Inited) OnOpen(); }
    internal void NotifyClose()  { if (Inited) OnClose(); }
    internal void NotifyShow()   { if (Inited) OnShow(); }
    internal void NotifyHide()   { if (Inited) OnHide(); }
    internal void NotifyPause()  { if (Inited) OnPause(); }
    internal void NotifyResume() { if (Inited) OnResume(); }
}
```

---

## 八、UIPanel（Window 内子面板，纯C#类）

### 定位

| 类型 | 定位 | 管理者 | 参与全局栈 | 典型用途 |
|------|------|--------|---------|---------|
| `UIWindow` | 独立页面/弹窗 | UIManager | ✅ | 主菜单、背包、商店 |
| `UIPanel` | Window 内子面板 | 所属 UIWindow | ❌ | 商店内 Tab、背包分类 |
| `UIWidget` | 可复用组件 | UIWindow/UIPanel | ❌ | 道具格子、头像框 |

### UIPanel 基类

```csharp
public abstract class UIPanel
{
    public GameObject GameObject { get; private set; }
    public Transform Transform { get; private set; }
    public UIWindow OwnerWindow { get; private set; }
    public bool IsActive { get; private set; }

    public void Init(GameObject root, UIWindow owner)
    {
        GameObject = root;
        Transform = root.transform;
        OwnerWindow = owner;
        OnCreate();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        GameObject.SetActive(true);
        OnActivate();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        OnDeactivate();
        GameObject.SetActive(false);
    }

    public void Refresh() => OnRefresh();

    public void Destroy()
    {
        OnDestroy();
        IsActive = false;
        GameObject = null;
        Transform = null;
        OwnerWindow = null;
    }

    protected virtual void OnCreate() { }
    protected virtual void OnActivate() { }
    protected virtual void OnDeactivate() { }
    protected virtual void OnRefresh() { }
    protected virtual void OnDestroy() { }
}

// 有参版本
public abstract class UIPanel<TData> : UIPanel
{
    protected TData Data { get; private set; }

    public void Activate(TData data)
    {
        Data = data;
        Activate();
    }

    protected sealed override void OnActivate() => OnActivate(Data);
    protected abstract void OnActivate(TData data);
}
```

### 典型用法

```csharp
public class ShopWindow : UIWindow<ShopOpenData>
{
    private ShopItemPanel  _itemPanel  = new();
    private ShopEquipPanel _equipPanel = new();
    private UIPanel _currentPanel;

    protected override void OnCreate()
    {
        _itemPanel.Init(Transform.Find("Tabs/ItemTab").gameObject, this);
        _equipPanel.Init(Transform.Find("Tabs/EquipTab").gameObject, this);
    }

    protected override void OnOpen(ShopOpenData data)
    {
        SwitchPanel(data.Tab == ShopTab.Equip ? (UIPanel)_equipPanel : _itemPanel);
    }

    protected override void OnClose()
    {
        _currentPanel?.Deactivate();
        _itemPanel.Destroy();
        _equipPanel.Destroy();
    }

    public void SwitchPanel(UIPanel panel)
    {
        if (_currentPanel == panel) return;
        _currentPanel?.Deactivate();
        _currentPanel = panel;
        _currentPanel.Activate();
    }
}
```

### 层次结构

```
UIWindow（纯C#，全局栈管理）
├── UIPanel A（纯C#）→ GameObject: "Tabs/ItemTab"
│   └── UIWidget（纯C#，可复用组件）
├── UIPanel B（纯C#）→ GameObject: "Tabs/EquipTab"
└── UIWidget（直接挂 Window 的）
```

---

## 九、异步双轨（Coroutine + UniTask）

### 目录结构

```
Runtime/
├── Core/
│   └── UIManager.Open.cs           // Coroutine + callback（零依赖）
└── Extensions/
    └── UniTask/
        ├── UIManagerUniTaskExtensions.cs
        └── MUFramework.UniTask.asmdef
```

### UniTask asmdef 配置

```json
{
    "name": "MUFramework.UniTask",
    "references": ["MUIFramework", "UniTask"],
    "versionDefines": [{
        "name": "com.cysharp.unitask",
        "expression": "",
        "define": "UNITASK_SUPPORT"
    }]
}
```

### UniTask 扩展方法

```csharp
#if UNITASK_SUPPORT
public static class UIManagerUniTaskExtensions
{
    public static UniTask<TWindow> OpenAsync<TWindow>(
        this UIManager manager, CancellationToken ct = default)
        where TWindow : UIWindow
    {
        var tcs = new UniTaskCompletionSource<TWindow>();
        ct.Register(() => tcs.TrySetCanceled());
        manager.OpenAsync<TWindow>(w => tcs.TrySetResult(w));
        return tcs.Task;
    }

    public static UniTask<TWindow> OpenAsync<TWindow, TData>(
        this UIManager manager, TData data, CancellationToken ct = default)
        where TWindow : UIWindow<TData>
    {
        var tcs = new UniTaskCompletionSource<TWindow>();
        ct.Register(() => tcs.TrySetCanceled());
        manager.OpenAsync<TWindow, TData>(data, w => tcs.TrySetResult(w));
        return tcs.Task;
    }

    public static UniTask CloseAsync(
        this UIManager manager, long uniqueId, CancellationToken ct = default)
    {
        var tcs = new UniTaskCompletionSource();
        manager.Close(uniqueId, withAnimation: true, onComplete: () => tcs.TrySetResult());
        return tcs.Task;
    }
}
#endif
```

---

## 十、代码生成系统

两套独立的代码生成机制，职责明确，互不耦合：

| | SG（Source Generator） | Editor Code Generator |
|---|---|---|
| 触发时机 | 编译期 | 编辑器内手动/保存触发 |
| 信息来源 | C# 代码结构 | 预制体资产（Unity 序列化引用） |
| 负责内容 | `IOpenArgs` 动态分发 | 组件自动绑定 |
| 生成位置 | `.Generated.cs`（隐藏） | `.AutoBind.cs`（可见，可提交） |

---

### 10.1 Source Generator：IOpenArgs 动态分发

SG 只负责一件事：检测已继承的 `IOpenArgs<T...>` 接口，生成 `OnOpenInternal(object[])` 动态分发覆写，供完整 API / 热更路径使用。

**SG 不生成任何声明**——开发者自己继承接口、自己实现方法，SG 只做胶水层。

#### 目录结构

```
Runtime/
├── Core/
│   ├── Interfaces/
│   │   └── IOpenArgs.cs         // IOpenArgs<T1> ~ IOpenArgs<T1,T2,T3>（手写）
│   └── Attributes/
│       └── UIWindowConfigAttribute.cs
└── Analyzer/
    └── UIOpenArgsGenerator.cs   // Source Generator 本体，只处理 IOpenArgs
```

#### 触发条件与生成内容

`partial class` + 继承 `IOpenArgs<T...>` → 生成 `OnOpenInternal(object[])` 覆写：

```csharp
// 开发者手写
public partial class ShopWindow : UIWindow, IOpenArgs<int>, IOpenArgs<string, bool>
{
    public void OnOpen(int tab) { ... }
    public void OnOpen(string itemId, bool fromBattle) { ... }
}

// SG 生成（ShopWindow.Generated.cs）
public partial class ShopWindow
{
    internal override void OnOpenInternal(object[] args)
    {
        if (args.Length == 1 && args[0] is int t)
            { ((IOpenArgs<int>)this).OnOpen(t); return; }
        if (args.Length == 2 && args[0] is string id && args[1] is bool f)
            { ((IOpenArgs<string, bool>)this).OnOpen(id, f); return; }
        base.OnOpenInternal(args);
    }
}
```

---

### 10.2 Editor Code Generator：预制体组件自动绑定

#### 设计概述

在预制体根节点挂载 `UIBindingCollector`（MonoBehaviour），Editor 脚本扫描预制体子节点，按命名规则识别组件类型，序列化引用，触发代码生成器写出 `.AutoBind.cs`。

**两个钩子分离，职责清晰：**

```csharp
public abstract class UIWindow
{
    // AutoBind.cs 生成的覆写——预制体收集器生成，勿手动修改
    internal virtual void AutoBindComponents() { }

    // 开发者手写覆写——补充 AutoBind 无法处理的动态/运行时绑定
    protected virtual void BindComponents() { }

    public void Init(UIStackNode stackNode)
    {
        _stackNode = stackNode;
        AutoBindComponents();  // 先自动绑定
        BindComponents();      // 再手动补充
        if (_hasCreated) return;
        _hasCreated = true;
        OnCreate();
    }
}
```

#### UIBindingCollector

```csharp
// 挂在预制体根节点，Editor 专用
[DisallowMultipleComponent]
public class UIBindingCollector : MonoBehaviour
{
    // 序列化的绑定条目，Editor 扫描后填充，运行时只读
    [SerializeField] internal List<UIBindingEntry> Entries = new();

    // 目标类名（对应要生成的 Window/Panel/Widget）
    [SerializeField] internal string TargetClassName;
    [SerializeField] internal string TargetNamespace;
}

[Serializable]
public class UIBindingEntry
{
    public string FieldName;        // 生成的字段名，如 _closeButton
    public string ComponentType;    // 组件类型全名，如 UnityEngine.UI.Button
    public Component ComponentRef;  // 序列化引用（Editor 拖拽或自动扫描填充）
    public string Path;             // 相对路径，如 "Header/CloseButton"
}
```

#### 命名规则（可扩展）

框架提供默认规则，应用层继承覆写：

```csharp
// 框架内置默认规则
public class DefaultUIBindingNamingRule : UIBindingNamingRule
{
    // 前缀 → 组件类型映射
    public override IReadOnlyDictionary<string, Type> PrefixMap => new Dictionary<string, Type>
    {
        { "Btn_",   typeof(Button)      },
        { "Txt_",   typeof(TMP_Text)    },
        { "Img_",   typeof(Image)       },
        { "Raw_",   typeof(RawImage)    },
        { "Sld_",   typeof(Slider)      },
        { "Tog_",   typeof(Toggle)      },
        { "Inp_",   typeof(TMP_InputField) },
        { "Scr_",   typeof(ScrollRect)  },
        { "Rect_",  typeof(RectTransform) },
        { "Go_",    typeof(GameObject)  },  // 特殊：绑定 GameObject 本身
    };

    // 节点名 → 字段名转换规则（默认：Btn_Close → _btnClose）
    public override string ToFieldName(string nodeName, string prefix)
        => "_" + char.ToLower(prefix[0]) + prefix.Substring(1).TrimEnd('_')
               + nodeName.Substring(prefix.Length);
}

// 应用层扩展
public class MyNamingRule : DefaultUIBindingNamingRule
{
    public override IReadOnlyDictionary<string, Type> PrefixMap
    {
        get
        {
            var map = new Dictionary<string, Type>(base.PrefixMap);
            map["Panel_"] = typeof(MyCustomPanel);  // 项目自定义前缀
            return map;
        }
    }
}

// 注册（UIGlobal 初始化时）
UIGlobal.BindingNamingRule = new MyNamingRule();
```

#### 生成流程

```
预制体保存 / 手动点击 "Generate Bindings"
    ↓
UIBindingCollectorEditor 扫描所有子节点
    ↓
按 NamingRule 识别节点 → 填充 UIBindingCollector.Entries（序列化保存）
    ↓
UIBindingCodeGenerator 读取 Entries → 写出 ShopWindow.AutoBind.cs
```

#### 生成结果示例

预制体结构：
```
ShopWindow (root, 挂 UIBindingCollector, TargetClassName="ShopWindow")
├── Header/
│   └── Btn_Close      → Button
├── Txt_Title          → TMP_Text
└── Scr_ItemList       → ScrollRect
```

生成 `ShopWindow.AutoBind.cs`：
```csharp
// 自动生成，勿手动修改 —— 由 UIBindingCollector 在 Editor 中生成
public partial class ShopWindow
{
    private Button _btnClose;
    private TMP_Text _txtTitle;
    private ScrollRect _scrItemList;

    internal override void AutoBindComponents()
    {
        _btnClose    = transform.Find("Header/Btn_Close")?.GetComponent<Button>();
        _txtTitle    = transform.Find("Txt_Title")?.GetComponent<TMP_Text>();
        _scrItemList = transform.Find("Scr_ItemList")?.GetComponent<ScrollRect>();
    }
}
```

开发者手写 `ShopWindow.cs`：
```csharp
[UIWindowConfig(layer: UILayer.Normal)]
public partial class ShopWindow : UIWindow, IOpenArgs<int>
{
    // _btnClose / _txtTitle / _scrItemList 已由 AutoBind.cs 声明和绑定，直接用
    public void OnOpen(int tab) { ... }

    // 只需补充 AutoBind 无法处理的动态绑定
    protected override void BindComponents()
    {
        // 例：运行时动态创建的节点，或需要条件判断的绑定
    }

    protected override void OnCreate()
    {
        _btnClose.onClick.AddListener(OnCloseClick);
    }
}
```

#### UIPanel / UIWidget 同样支持

`UIBindingCollector` 上的 `TargetClassName` 可以指向 `UIPanel` 或 `UIWidget` 子类，生成逻辑完全一致：

```csharp
// ShopItemPanel 的预制体子节点挂 UIBindingCollector，TargetClassName="ShopItemPanel"
// 生成 ShopItemPanel.AutoBind.cs，覆写 UIPanel 的 AutoBindComponents()
public abstract class UIPanel
{
    internal virtual void AutoBindComponents() { }
    protected virtual void BindComponents() { }

    public void Init(GameObject root, UIWindow owner)
    {
        GameObject = root;
        Transform = root.transform;
        OwnerWindow = owner;
        AutoBindComponents();
        BindComponents();
        OnCreate();
    }
}
```

// 宿主 Window 调用
_itemPanel.Activate(ShopTab.Equipment);
```

---

## 十一、已知 Bug 修复清单

| # | 问题 | 修复方式 |
|---|------|---------|
| 1 | `Awake` 中 `DontDestroyOnLoad` 无守卫 | 加 `Application.isPlaying` 判断 |
| 2 | `UIWindow.Pause/Resume` 直接写 stackNode 状态（双写） | 状态写入职责收归 UIStackNode，Window 只提供回调钩子 |
| 3 | `OpenAsync` 强类型重载 `OnOpen` 被调两次 | OpenCore 是唯一调用点，callback 只回传引用 |
| 4 | `RecycleUIStackNode` 不销毁 GameObject | Dispose 前先 `Object.Destroy(go)` |
| 5 | `WindowOpenConfig` 是 struct 含 `List<>` 引用类型 | 改为 class |
| 6 | `CoverdBehavior` / `OnCovere` / `AttackGameObject` 拼写错误 | 统一修正 |
| 7 | `UIEventHandler` 从未触发 | 在各生命周期节点补全触发 |
| 8 | `Update` 驱动缺失 | `UIManager.Update.cs` 统一驱动活跃窗口 |
| 9 | 缓存过期清理缺失 | `CleanExpiredCache` 每5秒检查一次 |
| 10 | `HandleMsg` 空实现 | 实现为 `SendMessage`，通过 `OnMessageInternal` 分发 |

---

## 十二、文件变更清单

### 修改

| 文件 | 变更 |
|------|------|
| `UIManager.cs` | 拆为9个 partial + Awake守卫修复 |
| `UIWindow.cs` | 泛型基类 + 生命周期修正 + BindComponents 钩子 |
| `UIStackNode.cs` | Pause/Resume 双写修复 + Clear 时销毁 GO |
| `UIStack.cs` | GetTopNode 加 skipBackKeyCheck 参数 + 拼写修正 |
| `UIGlobal.cs` | UIEventHandler 签名升级为 `Action<string,string>` |
| `WindowOpenConfig.cs` | struct → class + 拼写修正 CoveredBehavior |
| `CoverdBehavior.cs` | 重命名为 `CoveredBehavior.cs` |
| `UIWidget.cs` | 补全 6 个生命周期回调 |

### 新增

| 文件 | 说明 |
|------|------|
| `UIWindow.cs` | 拆分为 UIWindow 基类 + `UIWindow<TData>` 泛型基类 |
| `UIPanel.cs` | UIPanel / UIPanel<TData> 纯C#子面板基类（独立，不继承UIWindow） |
| `Interfaces/IOpenArgs.cs` | `IOpenArgs<T1>` ~ `IOpenArgs<T1,T2,T3>` 接口定义（手写） |
| `Attributes/UIWindowConfigAttribute.cs` | `[UIWindowConfig(...)]` Attribute |
| `Analyzer/UIOpenArgsGenerator.cs` | Source Generator：只生成 IOpenArgs 动态分发 |
| `Editor/UIBindingCollector.cs` | MonoBehaviour，挂预制体根节点，序列化绑定条目 |
| `Editor/UIBindingEntry.cs` | 绑定条目数据结构 |
| `Editor/UIBindingNamingRule.cs` | 命名规则基类（可继承覆写） |
| `Editor/DefaultUIBindingNamingRule.cs` | 默认命名规则（Btn_/Txt_/Img_ 等） |
| `Editor/UIBindingCollectorEditor.cs` | Editor Inspector + 扫描触发逻辑 |
| `Editor/UIBindingCodeGenerator.cs` | 读取 Entries → 写出 `.AutoBind.cs` |
| `WindowRegistration.cs` | 注册表条目 |
| `UIManager.Open.cs` | Open partial（含 IOpenArgs 重载） |
| `UIManager.Close.cs` | Close partial |
| `UIManager.Query.cs` | Query partial |
| `UIManager.Message.cs` | Message partial |
| `UIManager.Registry.cs` | Registry partial |
| `UIManager.Cache.cs` | Cache partial |
| `UIManager.Update.cs` | Update partial |
| `UIManager.NodePool.cs` | NodePool partial |
| `Extensions/UniTask/UIManagerUniTaskExtensions.cs` | UniTask 扩展（可选） |
| `Extensions/UniTask/MUFramework.UniTask.asmdef` | UniTask 程序集（可选） |
