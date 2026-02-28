# MUFramework UI框架 - 待完善功能分析文档

> 分析日期：2026-02-28

## 概览

MUFramework 是一个基于 Unity UGUI 的 UI 管理框架，提供了窗口栈管理、多实例控制、依赖管理、缓存机制、动画系统等功能。当前框架的基础架构已搭建完成，但仍有若干核心功能尚未实现或存在缺陷。

---

## ~~P0 - Bug~~ (已修复)

### ~~1. OpenAsyncCoroutine 缺少 AttachGameObject 调用~~ ✅ 已修复

---

## P1 - 核心功能缺失

### ~~2. OpenBehavior 开启行为未实现~~ ✅ 已修复

### ~~3. CoverdBehavior 被覆盖行为未实现~~ ✅ 已修复

### ~~4. 关闭窗口时的恢复逻辑未实现~~ ✅ 已修复

**实现方式**：在 `UIStack.UpdateAllStackNode` 中通过单循环从栈顶到栈底计算所有节点的覆盖状态，取 `max(coverLevel, WhenCovered)` 决定最终行为（Keep/Pause/Hide）。`UIManager.OpenCore` 和 `Close` 回调中调用该方法触发重新计算。

---

### 5. UIEventHandler 事件从未触发

**文件**：`Runtime/Core/UIGlobal.cs`、`Runtime/Core/UIManager.cs`

**问题描述**：`UIGlobal` 定义了事件处理器和事件常量：

```csharp
public static Action<string> UIEventHandler;

public const string UI_EVENT_WINDOW_OPEN = "UI_EVENT_WINDOW_OPEN";
public const string UI_EVENT_WINDOW_CLOSE = "UI_EVENT_WINDOW_CLOSE";
public const string UI_EVENT_WINDOW_SHOW = "UI_EVENT_WINDOW_SHOW";
public const string UI_EVENT_WINDOW_HIDE = "UI_EVENT_WINDOW_HIDE";
public const string UI_EVENT_WINDOW_RESUME = "UI_EVENT_WINDOW_RESUME";
public const string UI_EVENT_WINDOW_PAUSE = "UI_EVENT_WINDOW_PAUSE";
public const string UI_EVENT_WINDOW_DESTROY = "UI_EVENT_WINDOW_DESTROY";
```

但 `UIManager` 中没有任何地方调用 `UIEventHandler` 来分发这些事件，外部系统无法监听 UI 生命周期。

**修复建议**：在 `UIWindow` 的各生命周期方法（`Open`、`Close`、`Show`、`Hide`、`Resume`、`Pause`、`Destroy`）中触发对应事件。

---

## P2 - 重要功能缺失

### 6. HandleMsg 窗口间消息传递未实现

**文件**：`Runtime/Core/UIManager.cs` 第 443-451 行

**问题描述**：两个 `HandleMsg` 重载方法体均为空：

```csharp
public void HandleMsg(string windowId, string msg, params object[] args)
{
    // 空实现
}

public void HandleMsg(long uniqueId, string msg, params object[] args)
{
    // 空实现
}
```

同时 `UIWindow` 中缺少对应的 `OnMessage` 虚方法来接收消息。

**修复建议**：
1. 在 `UIWindow` 中添加 `virtual void OnMessage(string msg, params object[] args)` 虚方法
2. 在 `UIManager.HandleMsg` 中查找目标窗口并调用其 `OnMessage`

---

### 7. Update 驱动循环缺失

**文件**：`Runtime/Core/UIManager.cs`

**问题描述**：`UIManager`（继承 MonoBehaviour）没有 `Update()` 或 `LateUpdate()` 方法。`UIWindow` 定义了 `Update(float deltaTime)` 和 `UpdatePerSecond()` 方法，但没有驱动者调用它们。

> 注：`UIWidgetMonoAdapter` 仅驱动 Widget 的更新，不驱动 Window。

**修复建议**：在 `UIManager` 中添加 `Update()` 方法，遍历所有活跃窗口调用其 `Update` 和 `UpdatePerSecond`。

---

### 8. 缓存过期清理未实现

**文件**：`Runtime/Core/UIManager.cs`

**问题描述**：`Close` 方法中会为 `CacheType.ExpireTime` 类型的窗口设置过期时间：

```csharp
node.SetExpireTime(Time.unscaledTimeAsDouble + node.OpenConfig.ExpireTime);
```

但框架中没有任何定时检查逻辑来清理 `_cachedUI` 中过期的 UI 实例。过期缓存会一直占用内存，形成内存泄漏。

**修复建议**：在 `Update()` 循环中定期检查 `_cachedUI` 中的过期项，对过期的缓存执行销毁和清理。

---

## P3 - 改进项

### 9. UIPanel 差异化逻辑缺失

**文件**：`Runtime/Core/Base/UIPanel.cs`

**问题描述**：`UIPanel` 当前为空壳类，仅继承 `UIWindow`，注释声明"Panel通常不参与栈管理"，但实际没有任何差异化逻辑。`UIManager` 中也没有对 Panel 类型做特殊处理。

**待确认**：Panel 与 Window 的具体行为差异是什么？是否需要独立的管理逻辑？

---

### 10. SafeAreaAdapter 未集成

**文件**：`Runtime/Utils/SafeAreaAdapter.cs`

**问题描述**：`SafeAreaAdapter.AdaptSafeArea()` 已实现安全区适配逻辑（查找名为 "SafeAreaContent" 的子节点并调整锚点），但框架中没有任何地方自动调用它。

**修复建议**：在 `OpenCore` 中窗口创建后自动调用 `SafeAreaAdapter.AdaptSafeArea(node.GameObject)`。

---

### 11. 拼写错误

| 当前拼写 | 正确拼写 | 位置 |
|----------|----------|------|
| ~~`PauseBleow`~~ | `PauseBelow` | `OpenBehavior.cs` (已修复) |
| `AttackGameObject` | `AttachGameObject` | `UIStackNode.cs` |
| `RecaculateSortingOrder` | `RecalculateSortingOrder` | `UIStack.cs` |
| `CoverdBehavior` | `CoveredBehavior` | `CoverdBehavior.cs` (枚举名及文件名) |

---

## 架构总览

```
UIManager (MonoBehaviour, 单例)
├── _layerStacks: Dictionary<UILayer, UIStack>      // 每层一个栈
├── _windowInstances: Dictionary<string, List<UIStackNode>>  // 按 WindowId 分组
├── _allWindows: Dictionary<long, UIStackNode>       // 按 UniqueId 索引
├── _cachedUI: Dictionary<string, UIStackNode>       // 缓存池
├── _uiStackNodePool: Stack<UIStackNode>             // 对象池
└── _asyncOpenCoroutines: Dictionary<long, Coroutine> // 异步加载协程

UIStack (每层一个)
├── _stack: List<UIStackNode>        // 有序栈
└── _nodeDict: Dictionary<long, UIStackNode>  // 快速查找

UIStackNode (栈节点，可复用)
├── OpenConfig, Window, State, UniqueId ...
└── Canvas, CanvasGroup (用于排序和交互控制)

UIWindow (纯 C# 类，非 MonoBehaviour)
├── 生命周期: OnCreate → OnOpen → OnShow → OnUpdate → OnPause → OnHide → OnClose → OnDestroy
└── Widget 管理: AttachWidget, _widgets

UIWidget → UIWidget<TData> → UIWidget<TData, TAttachedWindow>
└── 可通过 UIWidgetMonoAdapter 获得 MonoBehaviour 驱动
```

---

## 已完成功能

- ✅ 同步/异步窗口打开（基础流程）
- ✅ 窗口关闭（含动画）
- ✅ 多实例控制与溢出策略
- ✅ 依赖管理（缺失依赖的多种策略）
- ✅ UI 缓存（Persistent / ExpireTime / None）
- ✅ 层级管理与 SortingOrder 自动计算
- ✅ UI 动画接口（IUIAnimation）
- ✅ 返回键处理（HandleBackKey）
- ✅ UIWidget 系统（含泛型数据绑定）
- ✅ UIStackNode 对象池
- ✅ 资源加载抽象（IUIResourceLoader）
- ✅ 安全区适配工具（未集成）
- ✅ Canvas / EventSystem 自动创建
