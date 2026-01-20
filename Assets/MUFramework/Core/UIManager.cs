using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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

        /// <summary>
        /// 所有层级的栈管理
        /// </summary>
        private Dictionary<UILayer, UIStack> _layerStacks = new Dictionary<UILayer, UIStack>();

        /// <summary>
        /// 所有层级的Canvas
        /// </summary>
        private Dictionary<UILayer, Canvas> _layerCanvases = new Dictionary<UILayer, Canvas>();

        /// <summary>
        /// 所有窗口实例（按UniqueId索引）
        /// </summary>
        private Dictionary<string, StackNode> _allWindows = new Dictionary<string, StackNode>();

        /// <summary>
        /// 窗口配置缓存
        /// </summary>
        private Dictionary<string, WindowConfig> _windowConfigs = new Dictionary<string, WindowConfig>();

        /// <summary>
        /// 资源加载器
        /// </summary>
        public IUIResourceLoader ResourceLoader { get; set; }

        /// <summary>
        /// 配置JSON路径
        /// </summary>
        public string ConfigJsonPath { get; set; } = "WindowConfigs.json";

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化UIManager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // 初始化所有层级
            InitializeLayers();

            // 加载配置
            LoadConfigs();

            _isInitialized = true;
        }

        /// <summary>
        /// 初始化所有层级
        /// </summary>
        private void InitializeLayers()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                CreateLayer(layer);
            }
        }

        /// <summary>
        /// 创建层级
        /// </summary>
        private void CreateLayer(UILayer layer)
        {
            // 创建Canvas
            GameObject canvasGO = new GameObject($"Canvas_{layer}");
            canvasGO.transform.SetParent(transform);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = (int)layer;

            // 添加CanvasScaler
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 添加GraphicRaycaster
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            _layerCanvases[layer] = canvas;

            // 创建栈
            LayerConfig layerConfig = new LayerConfig
            {
                layer = layer,
                sortingOrder = (int)layer,
                enabled = true
            };
            UIStack stack = new UIStack(layerConfig);
            _layerStacks[layer] = stack;
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfigs()
        {
            _windowConfigs = WindowConfigLoader.LoadConfigs(ConfigJsonPath);
        }

        /// <summary>
        /// 打开窗口（同步）
        /// </summary>
        public UIWindow OpenWindow<T>(string windowId, UILayer? layer = null, string parentId = null, bool useAnimation = false, OverlayConfig overlayConfig = null) where T : UIWindow
        {
            return OpenWindow(typeof(T), windowId, layer, parentId, useAnimation, overlayConfig);
        }

        /// <summary>
        /// 打开窗口（同步）
        /// </summary>
        public UIWindow OpenWindow(Type windowType, string windowId, UILayer? layer = null, string parentId = null, bool useAnimation = false, OverlayConfig overlayConfig = null)
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
            GameObject prefab = ResourceLoader?.LoadResource(resourcePath);
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
            StackNode node = new StackNode
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
                yield return StartCoroutine(ResourceLoader.LoadResourceAsync(resourcePath, (go) => { prefab = go; }));
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
            StackNode node = new StackNode
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
        public void CloseWindow(string uniqueId)
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
        private StackNode GetNode(string uniqueId)
        {
            _allWindows.TryGetValue(uniqueId, out var node);
            return node;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        private WindowConfig GetConfig(string windowId)
        {
            _windowConfigs.TryGetValue(windowId, out var config);
            return config;
        }

        /// <summary>
        /// 检查多实例
        /// </summary>
        private bool CheckMultiInstance(string windowId, WindowConfig config)
        {
            if (config == null)
                return true;

            if (config.AllowMultiInstance)
                return true;

            // 统计当前实例数
            int instanceCount = 0;
            foreach (var node in _allWindows.Values)
            {
                if (node.Window.WindowId == windowId)
                    instanceCount++;
            }

            if (instanceCount >= config.MaxInstances)
            {
                // 处理溢出
                switch (config.OverflowPolicy)
                {
                    case OverflowPolicy.Reject:
                        Debug.LogWarning($"Window {windowId} reached max instances ({config.MaxInstances}), rejecting");
                        return false;

                    case OverflowPolicy.CloseOldest:
                        // 关闭最旧的实例
                        CloseOldestInstance(windowId);
                        break;

                    case OverflowPolicy.CloseNewest:
                        // 关闭最新的实例
                        CloseNewestInstance(windowId);
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// 关闭最旧的实例
        /// </summary>
        private void CloseOldestInstance(string windowId)
        {
            StackNode oldest = null;
            foreach (var node in _allWindows.Values)
            {
                if (node.Window.WindowId == windowId)
                {
                    if (oldest == null || string.Compare(node.UniqueId, oldest.UniqueId) < 0)
                    {
                        oldest = node;
                    }
                }
            }
            if (oldest != null)
            {
                CloseWindow(oldest.UniqueId);
            }
        }

        /// <summary>
        /// 关闭最新的实例
        /// </summary>
        private void CloseNewestInstance(string windowId)
        {
            StackNode newest = null;
            foreach (var node in _allWindows.Values)
            {
                if (node.Window.WindowId == windowId)
                {
                    if (newest == null || string.Compare(node.UniqueId, newest.UniqueId) > 0)
                    {
                        newest = node;
                    }
                }
            }
            if (newest != null)
            {
                CloseWindow(newest.UniqueId);
            }
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        private string GenerateUniqueId(string windowId)
        {
            return $"{windowId}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>
        /// 获取资源路径
        /// </summary>
        private string GetResourcePath(string windowId)
        {
            // 默认路径，可以由配置或调用方指定
            return $"UI/{windowId}";
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
            StackNode topNode = stack.Top;

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
        public UIStack GetStack(UILayer layer)
        {
            _layerStacks.TryGetValue(layer, out var stack);
            return stack;
        }
    }
}
