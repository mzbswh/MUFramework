# MUFramework UIManager Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 UIManager 拆分为 9 个 partial 文件；补全 Registry/Attribute 注册、GetWindow/TryGetWindow/SendMessage 新 API；实现 Update 驱动、缓存过期清理；触发 UIEventHandler。

**Architecture:** UIManager 保持单例 MonoBehaviour，按职责拆为 partial class 文件，共享私有字段。Registry 通过反射扫描 `[UIWindowConfig]` Attribute 实现。缓存容器从 Stack 改为 List 支持过期遍历。

**Tech Stack:** C# partial class、System.Reflection、Unity NUnit EditMode Tests

**先决条件:** Plan 1 已完成（IOpenArgs、UIWindow\<TData\>、拼写修正均已就位）

---

## 文件变更清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `Runtime/Core/UIManager.cs` | 保留字段、单例、Awake、Initialize、共享 private 方法 |
| 新增 | `Runtime/Core/UIManager.Open.cs` | Open 同步/异步 + OpenCore |
| 新增 | `Runtime/Core/UIManager.Close.cs` | Close、CloseAll、CancelAsync |
| 新增 | `Runtime/Core/UIManager.Query.cs` | GetWindow、TryGetWindow、IsOpen、ExistUI |
| 新增 | `Runtime/Core/UIManager.Message.cs` | SendMessage、HandleBackKey |
| 新增 | `Runtime/Core/UIManager.Registry.cs` | Register、ScanAndRegisterAll、WindowId 推导 |
| 新增 | `Runtime/Core/UIManager.Cache.cs` | AddUIToCache、TryGetCachedUI、CleanExpiredCache |
| 新增 | `Runtime/Core/UIManager.Update.cs` | Update 驱动循环、UpdatePerSecond、过期检查 |
| 新增 | `Runtime/Core/UIManager.NodePool.cs` | CreateUIStackNode、RecycleUIStackNode、GetNewUIStackNode |
| 新增 | `Runtime/Core/WindowRegistration.cs` | 注册表条目数据结构 |
| 新增 | `Runtime/Core/Attributes/UIWindowConfigAttribute.cs` | [UIWindowConfig] Attribute |
| 修改 | `Tests/Runtime/UIManagerTests.cs` | 补充新 API 测试 |

---

## Task 1：新增 WindowRegistration + UIWindowConfigAttribute

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/WindowRegistration.cs`
- Create: `Assets/MUFramework/Runtime/Core/Attributes/UIWindowConfigAttribute.cs`

- [ ] **Step 1: 创建 WindowRegistration.cs**

```csharp
// Assets/MUFramework/Runtime/Core/WindowRegistration.cs
using System;

namespace MUFramework
{
    public sealed class WindowRegistration
    {
        public string WindowId { get; }
        public Type WindowType { get; }
        public WindowOpenConfig DefaultConfig { get; }

        public WindowRegistration(string windowId, Type windowType, WindowOpenConfig defaultConfig)
        {
            WindowId = windowId;
            WindowType = windowType;
            DefaultConfig = defaultConfig;
        }
    }
}
```

- [ ] **Step 2: 创建 UIWindowConfigAttribute.cs**

```csharp
// Assets/MUFramework/Runtime/Core/Attributes/UIWindowConfigAttribute.cs
using System;

namespace MUFramework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UIWindowConfigAttribute : Attribute
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
        public WindowAttr WindowAttr { get; set; } = WindowAttr.None;

        public WindowOpenConfig ToConfig(string resolvedWindowId) => new WindowOpenConfig
        {
            WindowId = resolvedWindowId,
            Layer = Layer,
            WhenCovered = WhenCovered,
            OpenBehavior = OpenBehavior,
            CacheType = CacheType,
            ExpireTime = ExpireTime,
            AllowMultiInstance = AllowMultiInstance,
            MaxInstances = MaxInstances,
            OverflowPolicy = OverflowPolicy,
            WindowAttr = WindowAttr,
        };
    }
}
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/WindowRegistration.cs
git add Assets/MUFramework/Runtime/Core/Attributes/UIWindowConfigAttribute.cs
git commit -m "feat: add WindowRegistration and UIWindowConfigAttribute"
```

---

## Task 2：UIManager.Registry.cs — Register + ScanAndRegisterAll

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Registry.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`（添加 _registry 字段）

- [ ] **Step 1: 在 UIManager.cs 字段区添加 _registry**

在 `UIManager.cs` 的字段声明区追加：

```csharp
private readonly Dictionary<string, WindowRegistration> _registry = new();
```

- [ ] **Step 2: 创建 UIManager.Registry.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Registry.cs
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MUFramework
{
    public partial class UIManager
    {
        /// <summary> 手动注册一个窗口配置 </summary>
        public void Register(WindowRegistration registration)
        {
            if (registration == null) return;
            _registry[registration.WindowId] = registration;
        }

        /// <summary>
        /// 扫描所有已加载程序集，收集带 [UIWindowConfig] 的 UIWindow 子类并注册。
        /// Initialize() 内自动调用，也可手动重新触发。
        /// </summary>
        public void ScanAndRegisterAll()
        {
            var windowType = typeof(UIWindow);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 跳过系统程序集
                var name = assembly.FullName;
                if (name.StartsWith("Unity") || name.StartsWith("System")
                    || name.StartsWith("mscorlib") || name.StartsWith("Mono"))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract || !windowType.IsAssignableFrom(type)) continue;
                    var attr = type.GetCustomAttribute<UIWindowConfigAttribute>(inherit: false);
                    if (attr == null) continue;

                    var windowId = string.IsNullOrEmpty(attr.WindowId) ? type.Name : attr.WindowId;
                    var config = attr.ToConfig(windowId);
                    Register(new WindowRegistration(windowId, type, config));
                }
            }
        }

        /// <summary>
        /// 获取注册配置（克隆一份，避免外部修改影响注册表）。
        /// 找不到时返回 null。
        /// </summary>
        internal WindowRegistration GetRegistration(string windowId)
        {
            _registry.TryGetValue(windowId, out var reg);
            return reg;
        }

        /// <summary>
        /// 获取注册配置副本，并可选覆写部分字段。
        /// </summary>
        internal WindowOpenConfig ResolveConfig(string windowId, Action<WindowOpenConfig> configOverride = null)
        {
            var reg = GetRegistration(windowId);
            if (reg == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error,
                    $"[MUI] Window '{windowId}' not registered. Call Register() or ScanAndRegisterAll() first.");
                return null;
            }
            // 深拷贝默认配置
            var config = new WindowOpenConfig
            {
                WindowId       = reg.DefaultConfig.WindowId,
                Layer          = reg.DefaultConfig.Layer,
                WhenCovered    = reg.DefaultConfig.WhenCovered,
                OpenBehavior   = reg.DefaultConfig.OpenBehavior,
                CacheType      = reg.DefaultConfig.CacheType,
                ExpireTime     = reg.DefaultConfig.ExpireTime,
                AllowMultiInstance     = reg.DefaultConfig.AllowMultiInstance,
                MaxInstances   = reg.DefaultConfig.MaxInstances,
                OverflowPolicy = reg.DefaultConfig.OverflowPolicy,
                WindowAttr     = reg.DefaultConfig.WindowAttr,
                UIAnimation    = reg.DefaultConfig.UIAnimation,
                Dependencies   = reg.DefaultConfig.Dependencies,
                DependencyMissingPolicy = reg.DefaultConfig.DependencyMissingPolicy,
            };
            configOverride?.Invoke(config);
            return config;
        }
    }
}
```

- [ ] **Step 3: 在 UIManager.Initialize() 中调用 ScanAndRegisterAll**

在 `UIManager.cs` 的 `Initialize()` 末尾追加：

```csharp
ScanAndRegisterAll();
```

- [ ] **Step 4: 编译验证**

- [ ] **Step 5: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git add Assets/MUFramework/Runtime/Core/UIManager.Registry.cs
git commit -m "feat: UIManager.Registry - Register, ScanAndRegisterAll, ResolveConfig"
```

---

## Task 3：UIManager.Open.cs — 简化 API（无 config 重载）

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Open.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`（保留旧 Open/OpenAsync，新增 partial 声明）

- [ ] **Step 1: 在 UIManager.cs 顶部添加 partial 关键字**

确认 `UIManager.cs` 的类声明为：
```csharp
public partial class UIManager : MonoBehaviour
```

- [ ] **Step 2: 创建 UIManager.Open.cs（简化 API 层）**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Open.cs
using System;
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        // ── 简化 API：无参 ──────────────────────────────

        public TWindow Open<TWindow>() where TWindow : UIWindow
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config) as TWindow;
        }

        // ── 简化 API：UIWindow<TData> 路径 ──────────────

        public TWindow Open<TWindow, TData>(TData data)
            where TWindow : UIWindow<TData>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config, data) as TWindow;
        }

        public TWindow Open<TWindow, TData>(TData data, Action<WindowOpenConfig> configOverride)
            where TWindow : UIWindow<TData>
        {
            var config = ResolveConfig(typeof(TWindow).Name, configOverride);
            if (config == null) return null;
            return Open(config, data) as TWindow;
        }

        // ── 简化 API：IOpenArgs 多参数路径 ───────────────

        public TWindow Open<TWindow, T1>(T1 arg1)
            where TWindow : UIWindow, IOpenArgs<T1>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config, arg1) as TWindow;
        }

        public TWindow Open<TWindow, T1, T2>(T1 arg1, T2 arg2)
            where TWindow : UIWindow, IOpenArgs<T1, T2>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config, arg1, arg2) as TWindow;
        }

        public TWindow Open<TWindow, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
            where TWindow : UIWindow, IOpenArgs<T1, T2, T3>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config, arg1, arg2, arg3) as TWindow;
        }

        // ── 简化 API：异步无参 ───────────────────────────

        public long OpenAsync<TWindow>(Action<TWindow> callback) where TWindow : UIWindow
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) { callback?.Invoke(null); return -1; }
            return OpenAsync(config, w => callback?.Invoke(w as TWindow));
        }

        // ── 简化 API：异步 UIWindow<TData> 路径 ─────────

        public long OpenAsync<TWindow, TData>(TData data, Action<TWindow> callback)
            where TWindow : UIWindow<TData>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) { callback?.Invoke(null); return -1; }
            return OpenAsync(config, w => callback?.Invoke(w as TWindow), data);
        }

        // ── 简化 API：异步 IOpenArgs 路径 ────────────────

        public long OpenAsync<TWindow, T1>(T1 arg1, Action<TWindow> callback)
            where TWindow : UIWindow, IOpenArgs<T1>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) { callback?.Invoke(null); return -1; }
            return OpenAsync(config, w => callback?.Invoke(w as TWindow), arg1);
        }

        public long OpenAsync<TWindow, T1, T2>(T1 arg1, T2 arg2, Action<TWindow> callback)
            where TWindow : UIWindow, IOpenArgs<T1, T2>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) { callback?.Invoke(null); return -1; }
            return OpenAsync(config, w => callback?.Invoke(w as TWindow), arg1, arg2);
        }
    }
}
```

- [ ] **Step 3: 确认 UIManager.cs 中的 OpenCore 调用唯一 OnOpenInternal**

在 `UIManager.cs` 内找到 `OpenCore` 方法，确保形如：

```csharp
private UIWindow OpenCore(UIStackNode node, object[] args)
{
    node.Window.Init(node);
    node.Window.OnOpenInternal(args ?? Array.Empty<object>());
    node.Window.Show();
    SafeAreaAdapter.AdaptSafeArea(node.GameObject);
    UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_OPEN, node.WindowId);
    if (TryGetUILayerStack(node.Layer, out var stack))
        stack.UpdateAllStackNode_Internal();   // 若不存在该方法，调用已有的 UpdateAllStackNode
    return node.Window;
}
```

- [ ] **Step 4: 编译验证**

- [ ] **Step 5: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.Open.cs
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "feat: UIManager.Open - simplified API layer using registry, IOpenArgs constraints"
```

---

## Task 4：UIManager.Query.cs — GetWindow / TryGetWindow / IsOpen

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Query.cs`

- [ ] **Step 1: 创建 UIManager.Query.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Query.cs
using System.Collections.Generic;

namespace MUFramework
{
    public partial class UIManager
    {
        /// <summary> 获取第一个匹配类型的窗口实例，不存在返回 null </summary>
        public TWindow GetWindow<TWindow>() where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return null;
            return nodes[0].Window as TWindow;
        }

        /// <summary> 尝试获取第一个匹配类型的窗口实例 </summary>
        public bool TryGetWindow<TWindow>(out TWindow window) where TWindow : UIWindow
        {
            window = GetWindow<TWindow>();
            return window != null;
        }

        /// <summary> 获取所有匹配类型的窗口实例（多实例场景） </summary>
        public IReadOnlyList<TWindow> GetWindows<TWindow>() where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return System.Array.Empty<TWindow>();
            var result = new List<TWindow>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Window is TWindow w) result.Add(w);
            }
            return result;
        }

        /// <summary> 指定类型的窗口是否在栈中 </summary>
        public bool IsOpen<TWindow>() where TWindow : UIWindow
            => ExistUI(typeof(TWindow).Name);

        /// <summary> 暂停指定 UniqueId 的窗口（供 UIWindow.Pause() 委托） </summary>
        internal void PauseWindow(long uniqueId)
        {
            var node = GetUIStackNode(uniqueId);
            node?.SetPause(true);
        }

        /// <summary> 恢复指定 UniqueId 的窗口（供 UIWindow.Resume() 委托） </summary>
        internal void ResumeWindow(long uniqueId)
        {
            var node = GetUIStackNode(uniqueId);
            node?.SetPause(false);
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.Query.cs
git commit -m "feat: UIManager.Query - GetWindow, TryGetWindow, GetWindows, IsOpen, PauseWindow, ResumeWindow"
```

---

## Task 5：UIManager.Message.cs — SendMessage + HandleBackKey

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Message.cs`

- [ ] **Step 1: 创建 UIManager.Message.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Message.cs
namespace MUFramework
{
    public partial class UIManager
    {
        /// <summary> 向指定类型的所有实例发送消息 </summary>
        public void SendMessage<TWindow>(string msg, params object[] args) where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null) return;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].IsLoaded)
                    nodes[i].Window.OnMessageInternal(msg, args);
            }
        }

        /// <summary> 向指定 UniqueId 的窗口发送消息 </summary>
        public void SendMessage(long uniqueId, string msg, params object[] args)
        {
            var node = GetUIStackNode(uniqueId);
            if (node != null && node.IsLoaded)
                node.Window.OnMessageInternal(msg, args);
        }

        /// <summary>
        /// 处理返回键：从最高层找到第一个不带 SkipBackKey 标记的栈顶窗口并关闭。
        /// 返回是否消耗了返回键事件。
        /// </summary>
        public bool HandleBackKey()
        {
            if (_uiLayers == null || _uiLayers.Length == 0) return false;
            for (int i = _uiLayers.Length - 1; i >= 0; i--)
            {
                if (!_layerStacks.TryGetValue(_uiLayers[i], out var stack)) continue;
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
    }
}
```

- [ ] **Step 2: 删除 UIManager.cs 中的旧 HandleMsg 空实现**

在 `UIManager.cs` 中删除：

```csharp
public void HandleMsg(string windowId, string msg, params object[] args) { }
public void HandleMsg(long uniqueId, string msg, params object[] args) { }
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.Message.cs
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "feat: UIManager.Message - SendMessage, HandleBackKey (replaces empty HandleMsg)"
```

---

## Task 6：UIManager.Cache.cs — 缓存容器 Stack→List + CleanExpiredCache

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Cache.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`（_cachedUI 类型改为 List）

- [ ] **Step 1: 修改 UIManager.cs 中 _cachedUI 字段类型**

```csharp
// 旧
private readonly Dictionary<string, Stack<UIStackNode>> _cachedUI = new();
// 新
private readonly Dictionary<string, List<UIStackNode>> _cachedUI = new();
```

- [ ] **Step 2: 创建 UIManager.Cache.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Cache.cs
using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        private void AddUIToCache(UIStackNode node)
        {
            if (node == null) return;
            if (!_cachedUI.TryGetValue(node.WindowId, out var list))
            {
                list = new List<UIStackNode>();
                _cachedUI[node.WindowId] = list;
            }
            list.Add(node);
            RemoveUIStackNode(node, recycle: false);
        }

        private UIStackNode TryGetCachedUI(string windowId)
        {
            if (!_cachedUI.TryGetValue(windowId, out var list)) return null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var node = list[i];
                if (node.Window != null && node.GameObject != null)
                {
                    list.RemoveAt(i);
                    return node;
                }
                list.RemoveAt(i);
                RecycleUIStackNode(node);
            }
            return null;
        }

        internal void CleanExpiredCache()
        {
            double now = Time.unscaledTimeAsDouble;
            foreach (var (_, list) in _cachedUI)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var node = list[i];
                    if (node.ExpireTime > 0 && now >= node.ExpireTime)
                    {
                        list.RemoveAt(i);
                        RecycleUIStackNode(node);
                    }
                }
            }
            _stringListCache.Clear();
            foreach (var key in _cachedUI.Keys)
                if (_cachedUI[key].Count == 0) _stringListCache.Add(key);
            foreach (var key in _stringListCache) _cachedUI.Remove(key);
        }
    }
}
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git add Assets/MUFramework/Runtime/Core/UIManager.Cache.cs
git commit -m "refactor: change _cachedUI Stack->List; add CleanExpiredCache"
```

---

## Task 7：UIManager.Update.cs — Update 驱动 + UpdatePerSecond + 过期清理

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Update.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`（添加 _updateSnapshot 字段）

- [ ] **Step 1: 在 UIManager.cs 字段区添加 Update 相关字段**

```csharp
private readonly List<UIStackNode> _updateSnapshot = new();
private float _perSecondTimer;
private float _cacheCleanTimer;
private const float PER_SECOND_INTERVAL = 1f;
private const float CACHE_CLEAN_INTERVAL = 5f;
```

- [ ] **Step 2: 创建 UIManager.Update.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Update.cs
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // 快照防止遍历中集合变更
            _updateSnapshot.Clear();
            foreach (var node in _allWindows.Values)
                _updateSnapshot.Add(node);

            for (int i = 0; i < _updateSnapshot.Count; i++)
            {
                var node = _updateSnapshot[i];
                if (node.IsLoaded && !node.IsPause && !node.IsClosing)
                    node.Window.Update(dt);
            }

            _perSecondTimer += dt;
            if (_perSecondTimer >= PER_SECOND_INTERVAL)
            {
                _perSecondTimer = 0f;
                for (int i = 0; i < _updateSnapshot.Count; i++)
                {
                    var node = _updateSnapshot[i];
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
    }
}
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git add Assets/MUFramework/Runtime/Core/UIManager.Update.cs
git commit -m "feat: UIManager.Update - drive Window.Update/UpdatePerSecond, trigger CleanExpiredCache every 5s"
```

---

## Task 8：UIManager.Close.cs — CloseAll + CancelAsync + UIEventHandler 触发

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.Close.cs`

- [ ] **Step 1: 创建 UIManager.Close.cs**

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.Close.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        /// <summary> 关闭指定类型的第一个实例 </summary>
        public void Close<TWindow>(bool withAnimation = true) where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return;
            Close(nodes[0].UniqueId, withAnimation);
        }

        /// <summary> 关闭指定类型的所有实例 </summary>
        public void CloseAll<TWindow>(bool withAnimation = true) where TWindow : UIWindow
        {
            Close(typeof(TWindow).Name, closeAllInstances: true, withAnimation: withAnimation);
        }

        /// <summary> 关闭所有已打开的窗口 </summary>
        public void CloseAll(bool withAnimation = true)
        {
            // 快照避免遍历中修改
            _stringListCache.Clear();
            foreach (var key in _windowInstances.Keys) _stringListCache.Add(key);
            foreach (var key in _stringListCache)
                Close(key, closeAllInstances: true, withAnimation: withAnimation);
        }

        /// <summary> 取消异步加载中的窗口 </summary>
        public bool CancelAsync(long uniqueId)
        {
            if (!_asyncOpenCoroutines.TryGetValue(uniqueId, out var coroutine)) return false;
            StopCoroutine(coroutine);
            _asyncOpenCoroutines.Remove(uniqueId);
            RemoveUIStackNode(uniqueId);
            return true;
        }
    }
}
```

- [ ] **Step 2: 在 UIManager.cs 的旧 Close(long) 回调中补全 UIEventHandler 触发**

在现有 `Close(long uniqueId, ...)` 方法的 `Hide` 回调中，`CompleteClose()` 之后追加：

```csharp
node.Window.CompleteClose();
UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_CLOSE, node.WindowId);
onComplete?.Invoke();
```

同理，在 `OpenCore` 的 `Show` 回调中（`OnShow` 之后）追加：

```csharp
UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_SHOW, node.WindowId);
```

在 `UIStackNode.SetPause` 通知路径追加触发（在 `UIManager` 内通过 `PauseWindow`/`ResumeWindow` 之后）：

```csharp
// PauseWindow
UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_PAUSE, node.WindowId);
// ResumeWindow
UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_RESUME, node.WindowId);
```

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.Close.cs
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "feat: UIManager.Close - CloseAll, CancelAsync; wire UIEventHandler triggers"
```

---

## Task 9：UIManager.NodePool.cs — partial 提取 NodePool 方法

**Files:**
- Create: `Assets/MUFramework/Runtime/Core/UIManager.NodePool.cs`
- Modify: `Assets/MUFramework/Runtime/Core/UIManager.cs`（移除对应方法，保留字段）

- [ ] **Step 1: 创建 UIManager.NodePool.cs**

将 `UIManager.cs` 中的 `CreateUIStackNode`、`CreateUIWindow`、`GetNewUIStackNode`、`RecycleUIStackNode` 方法剪切到新文件：

```csharp
// Assets/MUFramework/Runtime/Core/UIManager.NodePool.cs
using System;
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        private UIStackNode CreateUIStackNode(WindowOpenConfig openConfig, long uniqueId = -1)
        {
            UIStackNode node = GetNewUIStackNode();
            var window = CreateUIWindow(openConfig.WindowId);
            if (uniqueId < 0) uniqueId = GenerateUniqueId();
            node.Initialize(openConfig, window, uniqueId);
            return node;
        }

        private UIWindow CreateUIWindow(string windowId)
        {
            // 优先从注册表取类型
            var reg = GetRegistration(windowId);
            Type type = reg?.WindowType ?? UIGlobal.GetWindowClassTypeFunc?.Invoke(windowId);
            if (type == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error,
                    $"[MUI] Cannot resolve window type for '{windowId}'.");
                return null;
            }
            if (type.IsAbstract || !typeof(UIWindow).IsAssignableFrom(type))
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error,
                    $"[MUI] Invalid window type '{type}' for '{windowId}'.");
                return null;
            }
            return (UIWindow)Activator.CreateInstance(type);
        }

        private UIStackNode GetNewUIStackNode()
            => _uiStackNodePool.Count > 0 ? _uiStackNodePool.Pop() : new UIStackNode();

        private void RecycleUIStackNode(UIStackNode node)
        {
            if (node == null) return;
            var go = node.GameObject;
            node.Window?.Destroy();
            node.Dispose();
            if (go != null) UnityEngine.Object.Destroy(go);
            _uiStackNodePool.Push(node);
        }
    }
}
```

- [ ] **Step 2: 从 UIManager.cs 删除已移动的方法**

删除 `UIManager.cs` 中的 `CreateUIStackNode`、`CreateUIWindow`、`GetNewUIStackNode`、`RecycleUIStackNode` 方法体（字段 `_uiStackNodePool` 保留在 `UIManager.cs`）。

- [ ] **Step 3: 编译验证**

- [ ] **Step 4: Commit**

```bash
git add Assets/MUFramework/Runtime/Core/UIManager.NodePool.cs
git add Assets/MUFramework/Runtime/Core/UIManager.cs
git commit -m "refactor: extract NodePool methods to UIManager.NodePool.cs; CreateUIWindow uses registry first"
```

---

## Task 10：测试覆盖新 API

**Files:**
- Modify: `Assets/MUFramework/Tests/Runtime/UIManagerTests.cs`

- [ ] **Step 1: 添加 Registry 测试**

在 `UIManagerTests.cs` 末尾追加：

```csharp
// ===== Registry =====

[Test]
public void ScanAndRegisterAll_RegistersAttributeDecoratedWindow()
{
    // TestWindowWithAttr 在 TestHelpers 里标记了 [UIWindowConfig]
    Assert.IsTrue(_manager.GetRegistration_ForTest("TestWindowWithAttr") != null);
}

[Test]
public void Open_UsingSimplifiedAPI_NoConfig_OpensWindow()
{
    // 使用简化 API（无需手传 config），依赖注册表
    // TestWindowWithAttr 已注册，直接调用 Open<TestWindowWithAttr>()
    var w = _manager.Open<TestWindowWithAttr>();
    Assert.IsNotNull(w);
}
```

在 `TestHelpers.cs` 中添加：

```csharp
[UIWindowConfig(layer: UILayer.Normal, cache: CacheType.None)]
public class TestWindowWithAttr : UIWindow { }
```

并在 `UIManager.Registry.cs` 中临时暴露 `GetRegistration_ForTest`（仅测试用）：

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
internal WindowRegistration GetRegistration_ForTest(string windowId)
    => GetRegistration(windowId);
#endif
```

- [ ] **Step 2: 添加 GetWindow / TryGetWindow 测试**

```csharp
[Test]
public void GetWindow_AfterOpen_ReturnsInstance()
{
    _manager.Open(MakeConfig("TestWindow"));
    var w = _manager.GetWindow<TestWindow>();
    Assert.IsNotNull(w);
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
```

- [ ] **Step 3: 添加 SendMessage 测试**

在 `TestHelpers.cs` 中为 `TestWindow` 添加消息记录：

```csharp
public string LastMsg;
protected override void OnMessage(string msg, params object[] args)
    => LastMsg = msg;
```

在 `UIManagerTests.cs` 追加：

```csharp
[Test]
public void SendMessage_ReachesWindow()
{
    var w = _manager.Open(MakeConfig("TestWindow")) as TestWindow;
    _manager.SendMessage<TestWindow>("hello");
    Assert.AreEqual("hello", w.LastMsg);
}
```

- [ ] **Step 4: 运行所有测试，确认全部 PASS**

在 Unity Test Runner（EditMode）运行 `MUIFramework.Tests`。

- [ ] **Step 5: Commit**

```bash
git add Assets/MUFramework/Tests/Runtime/UIManagerTests.cs
git add Assets/MUFramework/Tests/Runtime/TestHelpers.cs
git commit -m "test: add Registry, GetWindow, IsOpen, SendMessage tests"
```

---

## 自检记录

**Spec coverage（Plan 2 范围）：**
- ✅ UIManager Partial 分拆（Task 1-9）
- ✅ Registry/Attribute 注册（Task 1, 2）
- ✅ 简化 API Open\<T\> / Open\<T,TData\> / Open\<T,T1,T2\>（Task 3）
- ✅ GetWindow、TryGetWindow、IsOpen（Task 4）
- ✅ SendMessage、HandleBackKey（Task 5）
- ✅ 缓存容器 Stack→List（Task 6）
- ✅ Update 驱动 + UpdatePerSecond + 过期清理（Task 7）
- ✅ CloseAll、CancelAsync（Task 8）
- ✅ UIEventHandler 触发（Task 8）
- ✅ NodePool 方法提取（Task 9）
- ✅ 新 API 测试（Task 10）

**未覆盖（Plan 3 处理）：**
- UIPanel 独立基类
- UIWidget 生命周期补全
