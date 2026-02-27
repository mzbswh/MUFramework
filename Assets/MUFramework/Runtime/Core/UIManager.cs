using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Codice.Client.Common.TreeGrouper;

namespace MUFramework
{
    /// <summary>
    /// UI管理器核心类
    /// 单例模式，管理所有UI界面
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary> 所有层级的栈管理 </summary>
        private readonly Dictionary<UILayer, UIStack> _layerStacks = new();

        /// <summary> 所有窗口实例栈节点列表（按WindowId索引，每个窗口ID可以对应多个实例） </summary>
        private readonly Dictionary<string, List<UIStackNode>> _windowInstances = new();

        /// <summary> 缓存的UI实例列表（按WindowId索引） </summary>
        private readonly Dictionary<string, Stack<UIStackNode>> _cachedUI = new();

        /// <summary> 所有窗口实例栈节点（按UniqueId索引） </summary>
        private readonly Dictionary<long, UIStackNode> _allWindows = new();

        /// <summary> UIStackNode对象池 </summary>
        private readonly Stack<UIStackNode> _uiStackNodePool = new();

        /// <summary> 正在进行的异步打开操作列表（按UniqueId索引） </summary>
        private readonly Dictionary<long, Coroutine> _asyncOpenCoroutines = new();

        /// <summary> 资源加载器 </summary>
        public IUIResourceLoader ResourceLoader { get; set; }

        /// <summary> 是否已初始化 </summary>
        private bool _isInitialized = false;

        /// <summary> 根Canvas </summary>
        private Transform _canvasRoot;

        private List<string> _stringListCache = new();  // 字符串列表缓存

        private long _uniqueIdCounter = 0; // 唯一ID计数器

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary> 初始化UIManager </summary>
        public void Initialize(Transform canvasRoot)
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _canvasRoot = canvasRoot;
            // 初始化所有层级
            InitializeLayers();
        }

        #region Open Sync

        /// <summary> 打开窗口(同步加载) </summary>
        public TWindow Open<TWindow>(WindowOpenConfig openConfig, params object[] args) where TWindow : UIWindow
        {
            var window = Open(openConfig, args);
            return window as TWindow;
        }

        public TWindow Open<TWindow, TArgs1>(WindowOpenConfig openConfig, TArgs1 arg1) where TWindow : UIWindow, IWindowOpenArgs<TArgs1>
        {
            var window = Open(openConfig, arg1);
            var ret = window as TWindow;
            ret.OnOpen(arg1);
            return ret;
        }

        public TWindow Open<TWindow, TArgs1, TArgs2>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2>
        {
            var window = Open(openConfig, arg1, arg2);
            var ret = window as TWindow;
            ret.OnOpen(arg1, arg2);
            return ret;
        }

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3>
        {
            var window = Open(openConfig, arg1, arg2, arg3);
            var ret = window as TWindow;
            ret.OnOpen(arg1, arg2, arg3);
            return ret;
        }

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3, TArgs4>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4>
        {
            var window = Open(openConfig, arg1, arg2, arg3, arg4);
            var ret = window as TWindow;
            ret.OnOpen(arg1, arg2, arg3, arg4);
            return ret;
        }

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4, TArgs5 arg5) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
        {
            var window = Open(openConfig, arg1, arg2, arg3, arg4, arg5);
            var ret = window as TWindow;
            ret.OnOpen(arg1, arg2, arg3, arg4, arg5);
            return ret;
        }

        /// <summary> 打开窗口(同步加载) </summary>
        public UIWindow Open(WindowOpenConfig openConfig, params object[] args)
        {
            // 先判断是否到达实例上限且配置为拒绝打开新实例，如果满足则直接返回
            if (!PreCheckMultiInstance(openConfig)) return null;
            // 检查依赖
            if (!HandleDependencies(openConfig)) return null;
            // 检查多实例溢出
            if (!HandleMultiInstance(openConfig)) return null;
            // 获取缓存的UI实例
            UIStackNode node = TryGetCachedUI(openConfig.WindowId);
            if (node == null)
            {
                // 同步加载资源
                GameObject gameObject = ResourceLoader?.LoadGameObject(openConfig.WindowId);
                if (gameObject == null)
                {
                    UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"Failed to load window resource: {openConfig.WindowId}");
                    return null;
                }
                node = CreateUIStackNode(openConfig);
                node.AttackGameObject(gameObject);
            }
            node.SetUniqueId(GenerateUniqueId());
            AddUIStackNode(node);
            return OpenCore(node, args);
        }

        #endregion Open Sync

        #region Open Async

        /// <summary> 打开窗口(异步加载) </summary>
        public long OpenAsync(WindowOpenConfig openConfig, Action<UIWindow> callback, params object[] args)
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                callback?.Invoke(window);
            }, args));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow>(WindowOpenConfig openConfig, Action<TWindow> callback, params object[] args) where TWindow : UIWindow
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                callback?.Invoke(window as TWindow);
            }, args));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1) where TWindow : UIWindow, IWindowOpenArgs<TArgs1>
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                var ret = window as TWindow;
                ret.OnOpen(arg1);
                callback?.Invoke(ret);
            }, arg1));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1, TArgs2>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2>
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                var ret = window as TWindow;
                ret.OnOpen(arg1, arg2);
                callback?.Invoke(ret);
            }, arg1, arg2));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3>
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                var ret = window as TWindow;
                ret.OnOpen(arg1, arg2, arg3);
                callback?.Invoke(ret);
            }, arg1, arg2, arg3));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3, TArgs4>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4>
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                var ret = window as TWindow;
                ret.OnOpen(arg1, arg2, arg3, arg4);
                callback?.Invoke(ret);
            }, arg1, arg2, arg3, arg4));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4, TArgs5 arg5) where TWindow : UIWindow, IWindowOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
        {
            var uniqueId = GenerateUniqueId();
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, (window) =>
            {
                _asyncOpenCoroutines.Remove(uniqueId);
                if (window == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                var ret = window as TWindow;
                ret.OnOpen(arg1, arg2, arg3, arg4, arg5);
                callback?.Invoke(ret);
            }, arg1, arg2, arg3, arg4, arg5));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        private IEnumerator OpenAsyncCoroutine(WindowOpenConfig openConfig, long uniqueId, Action<UIWindow> callback, params object[] args)
        {
            // 先判断是否到达实例上限且配置为拒绝打开新实例，如果满足则直接返回
            if (!PreCheckMultiInstance(openConfig))
            {
                callback?.Invoke(null);
                yield break;
            }
            // 检查依赖
            if (!HandleDependencies(openConfig))
            {
                callback?.Invoke(null);
                yield break;
            }
            // 检查多实例溢出
            if (!HandleMultiInstance(openConfig))
            {
                callback?.Invoke(null);
                yield break;
            }
            // 获取缓存的UI实例
            UIStackNode node = TryGetCachedUI(openConfig.WindowId);
            if (node != null)
            {
                // 从缓存中取出实例并打开
                node.SetUniqueId(uniqueId);
                AddUIStackNode(node);
                yield return null; // 等待一帧保证协程添加移除正确
                callback?.Invoke(OpenCore(node, args));
                yield break;
            }
            node = CreateUIStackNode(openConfig, uniqueId);
            AddUIStackNode(node);
            // 异步加载资源
            GameObject prefab = null;
            if (ResourceLoader != null)
            {
                yield return StartCoroutine(ResourceLoader.LoadGameObjectAsync(openConfig.WindowId, (go) => { prefab = go; }));
            }
            // 判断UI是否已被关闭
            if (!ExistUI(uniqueId))
            {
                callback?.Invoke(null);
                yield break;
            }
            if (prefab == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"Failed to load window resource async: {openConfig.WindowId}");
                callback?.Invoke(null);
                yield break;
            }
            var window = OpenCore(node, args);
            callback?.Invoke(window);
        }

        #endregion Open Async

        public void Close(string windowId, bool closeAllInstances = true, bool withAnimation = true, Action onComplete = null)
        {
            var instances = GetUIStackNodes(windowId);
            if (closeAllInstances)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    Close(instances[i].UniqueId, withAnimation, onComplete);
                }
            }
            else
            {
                if (instances.Count > 0)
                {
                    Close(instances[0].UniqueId, withAnimation, onComplete);
                }
            }
        }

        public void Close(long uniqueId, bool withAnimation = true, Action onComplete = null)
        {
            if (_asyncOpenCoroutines.TryGetValue(uniqueId, out var coroutine))
            {
                StopCoroutine(coroutine);
                _asyncOpenCoroutines.Remove(uniqueId);
                RemoveUIStackNode(uniqueId);
                return;
            }
            var node = GetNode(uniqueId);
            if (node == null) return;
            if (node.IsClosing || node.IsClosed) return;
            node.SetState(UIState.Closing);
            node.Window.SetInteractable(false);
            node.Window.Pause();
            node.Window.Hide(withAnimation, () =>
            {
                node.GameObject.SetActive(false);
                node.SetClosedState();
                node.Window.CompleteClose();
                onComplete?.Invoke();
                // 判断是否需要缓存
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
        }

        public bool ExistUI(long uniqueId)
        {
            return GetUIStackNode(uniqueId) != null;
        }


        /// <summary>
        /// 打开窗口（同步）
        /// </summary>
        public UIWindow OpenWindow(Type windowType, string windowId, UILayer? layer = null, string parentId = null, bool useAnimation = false, OverlayConfig? overlayConfig = null)
        {
            // 获取配置
            WindowConfig config = GetConfig(windowId);

            // 确定层级
            UILayer targetLayer = layer ?? (config?.DefaultLayer ?? UILayer.Default);

            // 检查依赖
            if (config != null && config.Dependencies != null && config.Dependencies.Count > 0)
            {
                foreach (var depId in config.Dependencies)
                {
                    if (!IsWindowOpen(depId))
                    {
                        // 打开依赖窗口（使用默认参数）
                        Debug.LogWarning($"Window {windowId} depends on {depId}, opening dependency first");
                        // 这里需要根据实际情况实现依赖窗口的打开逻辑
                    }
                }
            }

            // 检查多实例
            if (!CheckMultiInstance(windowId, config))
            {
                return null;
            }

            // 生成唯一ID
            string uniqueId = GenerateUniqueId(windowId);

            // 加载资源
            string resourcePath = GetResourcePath(windowId);
            GameObject prefab = ResourceLoader?.LoadGameObject(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load window resource: {resourcePath}");
                return null;
            }

            // 实例化
            GameObject instance = Instantiate(prefab, _layerCanvases[targetLayer].transform);
            UIWindow window = instance.GetComponent<UIWindow>();
            if (window == null)
            {
                window = instance.AddComponent(windowType) as UIWindow;
            }

            // 设置窗口属性
            window.UniqueId = uniqueId;
            window.WindowId = windowId;
            window.Layer = targetLayer;
            window.UseAnimation = useAnimation;

            // 创建栈节点
            UIStackNode node = new UIStackNode
            {
                UniqueId = uniqueId,
                Window = window,
                State = UIState.Hidden,
                Type = config?.WindowType ?? WindowType.Normal,
                WhenCovered = config?.DefaultCoverBehavior ?? CoverBehavior.KeepRunning,
                AllowOverrideCoverBehavior = config?.AllowOverrideCoverBehavior ?? true,
                ParentId = parentId,
                KeepBelowVisible = false,
                PauseBelowUpdate = false
            };

            // 处理界面绑定
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentNode = GetNode(parentId);
                if (parentNode != null)
                {
                    parentNode.ChildrenIds.Add(uniqueId);
                }
            }

            // 推入栈
            _layerStacks[targetLayer].Push(node);
            _allWindows[uniqueId] = node;

            // 更新状态
            UpdateWindowStates(targetLayer);

            // 调用生命周期
            window.OnCreate();
            window.OnShow();
            window.OnResume();

            return window;
        }

        /// <summary>
        /// 打开窗口（异步）
        /// </summary>
        public void OpenWindowAsync<T>(string windowId, UILayer? layer = null, string parentId = null, bool useAnimation = false, OverlayConfig overlayConfig = null, Action<UIWindow> callback = null) where T : UIWindow
        {
            StartCoroutine(OpenWindowAsyncCoroutine(typeof(T), windowId, layer, parentId, useAnimation, overlayConfig, callback));
        }

        /// <summary>
        /// 打开窗口异步协程
        /// </summary>
        private IEnumerator OpenWindowAsyncCoroutine(Type windowType, string windowId, UILayer? layer, string parentId, bool useAnimation, OverlayConfig overlayConfig, Action<UIWindow> callback)
        {
            // 获取配置
            WindowConfig config = GetConfig(windowId);

            // 确定层级
            UILayer targetLayer = layer ?? (config?.DefaultLayer ?? UILayer.Default);

            // 检查依赖
            if (config != null && config.Dependencies != null && config.Dependencies.Count > 0)
            {
                foreach (var depId in config.Dependencies)
                {
                    if (!IsWindowOpen(depId))
                    {
                        // 打开依赖窗口（使用默认参数）
                        Debug.LogWarning($"Window {windowId} depends on {depId}, opening dependency first");
                    }
                }
            }

            // 检查多实例
            if (!CheckMultiInstance(windowId, config))
            {
                callback?.Invoke(null);
                yield break;
            }

            // 生成唯一ID
            string uniqueId = GenerateUniqueId(windowId);

            // 异步加载资源
            string resourcePath = GetResourcePath(windowId);
            GameObject prefab = null;
            if (ResourceLoader != null)
            {
                yield return StartCoroutine(ResourceLoader.LoadGameOjbectAsync(resourcePath, (go) => { prefab = go; }));
            }

            if (prefab == null)
            {
                Debug.LogError($"Failed to load window resource: {resourcePath}");
                callback?.Invoke(null);
                yield break;
            }

            // 实例化
            GameObject instance = Instantiate(prefab, _layerCanvases[targetLayer].transform);
            UIWindow window = instance.GetComponent<UIWindow>();
            if (window == null)
            {
                window = instance.AddComponent(windowType) as UIWindow;
            }

            // 设置窗口属性
            window.UniqueId = uniqueId;
            window.WindowId = windowId;
            window.Layer = targetLayer;
            window.UseAnimation = useAnimation;

            // 创建栈节点
            OverlayConfig finalOverlayConfig = overlayConfig ?? OverlayConfig.Default;
            UIStackNode node = new UIStackNode
            {
                UniqueId = uniqueId,
                Window = window,
                State = UIState.Hidden,
                Type = config?.WindowType ?? WindowType.Normal,
                WhenCovered = finalOverlayConfig.WhenCovered,
                AllowOverrideCoverBehavior = finalOverlayConfig.AllowOverrideCoverBehavior,
                ParentId = parentId,
                KeepBelowVisible = finalOverlayConfig.KeepBelowVisible,
                PauseBelowUpdate = finalOverlayConfig.PauseBelowUpdate
            };

            // 处理界面绑定
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentNode = GetNode(parentId);
                if (parentNode != null)
                {
                    parentNode.ChildrenIds.Add(uniqueId);
                }
            }

            // 推入栈
            _layerStacks[targetLayer].Push(node);
            _allWindows[uniqueId] = node;

            // 更新状态
            UpdateWindowStates(targetLayer);

            // 调用生命周期
            window.OnCreate();
            window.OnShow();
            window.OnResume();

            callback?.Invoke(window);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow(long uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return;

            var node = GetNode(uniqueId);
            if (node == null)
                return;

            // 关闭所有子窗口
            if (node.ChildrenIds != null && node.ChildrenIds.Count > 0)
            {
                var childrenIds = new List<string>(node.ChildrenIds);
                foreach (var childId in childrenIds)
                {
                    CloseWindow(childId);
                }
            }

            // 调用生命周期
            node.Window.OnPause();
            node.Window.OnHide();
            node.Window.OnDestroy();

            // 从栈中移除
            _layerStacks[node.Window.Layer].Remove(uniqueId);
            _allWindows.Remove(uniqueId);

            // 更新状态
            UpdateWindowStates(node.Window.Layer);

            // 销毁GameObject
            if (node.Window != null)
            {
                Destroy(node.Window.gameObject);
            }
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        private UIStackNode GetNode(long uniqueId)
        {
            _allWindows.TryGetValue(uniqueId, out var node);
            return node;
        }

        /// <summary>
        /// 更新窗口状态
        /// </summary>
        private void UpdateWindowStates(UILayer layer)
        {
            var stack = _layerStacks[layer];
            if (stack == null || stack.IsEmpty)
                return;

            var nodes = stack.GetAllNodes();
            UIStackNode topNode = stack.Top;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isTop = (node == topNode);

                if (isTop)
                {
                    // 顶层窗口
                    if (node.State != UIState.VisibleAndActive)
                    {
                        node.State = UIState.VisibleAndActive;
                        node.Window.OnResume();
                    }
                }
                else
                {
                    // 非顶层窗口
                    if (topNode.KeepBelowVisible)
                    {
                        // 保持下层可见
                        if (node.State != UIState.VisibleButPaused)
                        {
                            node.State = UIState.VisibleButPaused;
                            node.Window.OnPause();
                        }
                    }
                    else
                    {
                        // 隐藏下层
                        if (node.State != UIState.Hidden)
                        {
                            node.State = UIState.Hidden;
                            node.Window.OnPause();
                            node.Window.OnHide();
                        }
                    }

                    // 暂停下层Update
                    if (topNode.PauseBelowUpdate)
                    {
                        if (node.State != UIState.VisibleButPaused && node.State != UIState.Hidden)
                        {
                            node.State = UIState.VisibleButPaused;
                            node.Window.OnPause();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查窗口是否打开
        /// </summary>
        public bool IsWindowOpen(string windowId)
        {
            foreach (var node in _allWindows.Values)
            {
                if (node.Window.WindowId == windowId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 插入窗口到指定位置
        /// </summary>
        public void InsertWindow(string uniqueId, int index, UILayer layer)
        {
            var node = GetNode(uniqueId);
            if (node == null)
                return;

            var stack = _layerStacks[layer];
            if (stack == null)
                return;

            stack.Remove(uniqueId);
            stack.Insert(index, node);
            UpdateWindowStates(layer);
        }

        /// <summary>
        /// 移除窗口（不销毁）
        /// </summary>
        public void RemoveWindow(string uniqueId)
        {
            var node = GetNode(uniqueId);
            if (node == null)
                return;

            _layerStacks[node.Window.Layer].Remove(uniqueId);
            _allWindows.Remove(uniqueId);
            UpdateWindowStates(node.Window.Layer);
        }

        /// <summary>
        /// 处理返回键
        /// </summary>
        public bool HandleBackKey()
        {
            // 按层级从高到低查找第一个可关闭界面
            UILayer[] layers = { UILayer.System, UILayer.Top, UILayer.Popup, UILayer.Default };

            foreach (var layer in layers)
            {
                var stack = _layerStacks[layer];
                if (stack == null || stack.IsEmpty)
                    continue;

                // 获取最上层的Normal类型窗口
                var topNode = stack.GetTopNormalNode();
                if (topNode != null && topNode.Window != null)
                {
                    topNode.Window.Close();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定层级的栈
        /// </summary>
        public bool TryGetUILayerStack(UILayer layer, out UIStack stack)
        {
            return _layerStacks.TryGetValue(layer, out stack);
        }

        /// <summary>
        /// 创建Canvas
        /// </summary>
        private void CreateCanvas()
        {
            Canvas = root.GetComponent<Canvas>();
            if (Canvas == null)
            {
                Canvas = root.AddComponent<Canvas>();
            }

            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = (int)Layer;

            // 添加CanvasScaler
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 添加GraphicRaycaster
            var raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }


        // ================================================

        public void HandleMsg(string windowId, string msg, params object[] args)
        {

        }

        public void HandleMsg(long uniqueId, string msg, params object[] args)
        {

        }

        /// <summary> 初始化所有层级 </summary>
        private void InitializeLayers()
        {
            var rootSortingLayer = _canvasRoot.GetComponent<Canvas>().sortingLayerID;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                CreateLayer(layer, rootSortingLayer);
            }
        }

        /// <summary> 创建层级 </summary>
        private void CreateLayer(UILayer layer, int rootSortingLayer)
        {
            // 创建Canvas
            GameObject canvasGO = new($"Canvas_{layer}");
            canvasGO.transform.SetParent(_canvasRoot);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.sortingLayerID = rootSortingLayer;
            canvas.sortingOrder = (int)layer * UIGlobal.LayerSortingOrderInterval;
            UIStack stack = new(layer, canvasGO);
            _layerStacks[layer] = stack;
        }

        /// <summary> 添加UIStackNode </summary>
        private void AddUIStackNode(UIStackNode node)
        {
            if (node == null) return;
            if (!_windowInstances.ContainsKey(node.WindowId))
            {
                _windowInstances[node.WindowId] = new List<UIStackNode>();
            }
            _windowInstances[node.WindowId].Add(node);
            _allWindows[node.UniqueId] = node;
            if (TryGetUILayerStack(node.Layer, out var stack))
            {
                stack.Push(node);
            }
        }

        private void RemoveUIStackNode(long uniqueId, bool recycle = true)
        {
            var node = GetUIStackNode(uniqueId);
            RemoveUIStackNode(node, recycle);
        }

        /// <summary> 移除UIStackNode </summary>
        private void RemoveUIStackNode(UIStackNode node, bool recycle = true)
        {
            if (node == null) return;
            if (_windowInstances.TryGetValue(node.WindowId, out var nodes))
            {
                nodes.Remove(node);
            }
            _allWindows.Remove(node.UniqueId);
            if (TryGetUILayerStack(node.Layer, out var stack))
            {
                stack.Remove(node.UniqueId);
            }
            if (recycle)
            {
                RecycleUIStackNode(node);
            }
        }

        /// <summary> 获取窗口实例数量 </summary>
        private int GetWindowInstanceCount(string windowId)
        {
            return _windowInstances.TryGetValue(windowId, out var nodes) ? nodes.Count : 0;
        }

        /// <summary> 获取窗口实例栈节点列表 </summary>
        private IReadOnlyList<UIStackNode> GetUIStackNodes(string windowId)
        {
            if (_windowInstances.TryGetValue(windowId, out var nodes))
            {
                return nodes;
            }
            return null;
        }

        /// <summary> 获取窗口实例栈节点 </summary>
        private UIStackNode GetUIStackNode(long uniqueId)
        {
            return _allWindows.TryGetValue(uniqueId, out var node) ? node : null;
        }

        /// <summary> 创建UIStackNode </summary>
        private UIStackNode CreateUIStackNode(WindowOpenConfig openConfig, long uniqueId = -1)
        {
            UIStackNode node = GetNewUIStackNode();
            var window = CreateUIWindow(openConfig.WindowId);
            if (uniqueId < 0)
            {
                uniqueId = GenerateUniqueId();
            }
            node.Initialize(openConfig, window, uniqueId);
            return node;
        }

        /// <summary> 创建UIWindow实例 </summary>
        private UIWindow CreateUIWindow(string windowId)
        {
            var type = UIGlobal.GetWindowClassTypeFunc?.Invoke(windowId);
            if (type == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], GetWindowClassTypeFunc returned null for windowId: {windowId}");
                return null;
            }
            if (type.IsAbstract || !typeof(UIWindow).IsAssignableFrom(type))
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], Invalid window class type for windowId: {windowId}, type: {type}");
                return null;
            }
            UIWindow window = (UIWindow)Activator.CreateInstance(type);
            return window;
        }

        /// <summary> 添加UI界面到缓存 </summary>
        private void AddUIToCache(UIStackNode node)
        {
            if (node == null) return;
            if (!_cachedUI.TryGetValue(node.WindowId, out var nodes))
            {
                nodes = new Stack<UIStackNode>();
                _cachedUI[node.WindowId] = nodes;
            }
            nodes.Push(node);
            RemoveUIStackNode(node, recycle: false);    // 移除但不回收
        }

        /// <summary> 获取缓存的UI界面 </summary>
        private UIStackNode TryGetCachedUI(string windowId)
        {
            if (_cachedUI.TryGetValue(windowId, out var nodes))
            {
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    if ((node.Window != null) && (node.GameObject != null))
                    {
                        return node;
                    }
                    RecycleUIStackNode(node);
                }
            }
            return null;
        }

        /// <summary> 获取UIStackNode </summary>
        private UIStackNode GetNewUIStackNode()
        {
            if (_uiStackNodePool.Count > 0)
            {
                return _uiStackNodePool.Pop();
            }
            return new UIStackNode();
        }

        /// <summary> 回收UIStackNode </summary>
        private void RecycleUIStackNode(UIStackNode node)
        {
            if (node == null) return;
            node.Dispose();
            _uiStackNodePool.Push(node);
        }

        /// <summary> 生成唯一ID </summary>
        private long GenerateUniqueId()
        {
            return ++_uniqueIdCounter;
        }

        /// <summary> 预先检查多实例是否通过 </summary>
        private bool PreCheckMultiInstance(WindowOpenConfig openConfig)
        {
            var maxInsCount = openConfig.AllowMultiInstance ? openConfig.MaxInstances : 1;
            var reachMaxInstance = GetWindowInstanceCount(openConfig.WindowId) >= maxInsCount;
            var ok = !reachMaxInstance || (openConfig.OverflowPolicy != OverflowPolicy.Reject);
            if (!ok)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Info, $"[MUI], Reject open window {openConfig.WindowId} because of max instances limit");
            }
            return ok;
        }

        /// <summary> 处理多实例溢出 </summary>
        private bool HandleMultiInstance(WindowOpenConfig openConfig)
        {
            var maxInsCount = openConfig.AllowMultiInstance ? openConfig.MaxInstances : 1;
            var reachMaxInstance = GetWindowInstanceCount(openConfig.WindowId) >= maxInsCount;
            if (!reachMaxInstance) return true;
            // 处理溢出
            switch (openConfig.OverflowPolicy)
            {
                case OverflowPolicy.Reject:
                    UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], HandleMultiInstance Reject open window {openConfig.WindowId} because of max instances limit");
                    return false;
                case OverflowPolicy.CloseOldest:
                    // 关闭最旧的实例
                    CloseOldestInstance(openConfig.WindowId);
                    break;
                case OverflowPolicy.CloseNewest:
                    // 关闭最新的实例
                    CloseNewestInstance(openConfig.WindowId);
                    break;
            }
            return true;
        }

        /// <summary> 关闭最旧的实例 </summary>
        private void CloseOldestInstance(string windowId)
        {
            var nodes = GetUIStackNodes(windowId);
            if ((nodes == null) || (nodes.Count == 0)) return;
            CloseWindow(nodes[0].UniqueId);
        }

        /// <summary> 关闭最新的实例 </summary>
        private void CloseNewestInstance(string windowId)
        {
            var nodes = GetUIStackNodes(windowId);
            if ((nodes == null) || (nodes.Count == 0)) return;
            CloseWindow(nodes[^1].UniqueId);
        }

        /// <summary> 处理依赖 </summary>
        private bool HandleDependencies(WindowOpenConfig openConfig)
        {
            if ((openConfig.Dependencies == null) || (openConfig.Dependencies.Count == 0)) return true;
            // 获取缺失的依赖界面ID列表
            _stringListCache.Clear();
            for (int i = 0; i < openConfig.Dependencies.Count; i++)
            {
                if (!IsWindowOpen(openConfig.Dependencies[i]))
                {
                    _stringListCache.Add(openConfig.Dependencies[i]);
                }
            }
            if (_stringListCache.Count <= 0) return true;
            // 处理依赖缺失
            if (openConfig.DependencyMissingPolicy == DependencyMissingPolicy.NotOpen)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], Reject open window {openConfig.WindowId} because of missing dependencies: {string.Join(", ", _stringListCache)}");
                return false;
            }
            switch (openConfig.DependencyMissingPolicy)
            {
                case DependencyMissingPolicy.OpenMissingDependency:
                {
                    for (int i = 0; i < _stringListCache.Count; i++)
                    {
                        var (succ, config) = UIGlobal.GetWindowOpenConfigFunc.Invoke(_stringListCache[i]);
                        if (succ)
                        {
                            Open(config);
                        }
                        else
                        {
                            UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], Open missing dependency window {_stringListCache[i]} failed, config is null");
                            return false;
                        }
                    }
                    break;
                }
                case DependencyMissingPolicy.ReOpenAllDependencies:
                {
                    for (int i = 0; i < openConfig.Dependencies.Count; i++)
                    {
                        var (succ, config) = UIGlobal.GetWindowOpenConfigFunc.Invoke(openConfig.Dependencies[i]);
                        if (succ)
                        {
                            Close(openConfig.Dependencies[i], closeAllInstances: true, withAnimation: false, onComplete: null);
                            Open(config);
                        }
                        else
                        {
                            UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], ReOpen missing dependency window {openConfig.Dependencies[i]} failed, config is null");
                            return false;
                        }
                    }
                    break;
                }
            }
            return true;
        }

        /// <summary> 打开界面核心方法 </summary>
        private UIWindow OpenCore(UIStackNode node, params object[] args)
        {
            // 获取目标层级
            UILayer targetLayer = node.OpenConfig.Layer;
            if (!_layerStacks.TryGetValue(targetLayer, out var stack))
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], OpenCore failed, target layer {targetLayer} stack not found");
                return null;
            }
            // 设置父对象
            var obj = node.GameObject;
            obj.transform.SetParent(stack.Root.transform, false);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.transform.localScale = Vector3.one;
            // 
            return null;
        }
    }
}
