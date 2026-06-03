using System.Collections.Generic;
using System;
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UI管理器核心类
    /// 单例模式，管理所有UI界面
    /// </summary>
    public partial class UIManager : MonoBehaviour
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
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public Camera UICamera { get; private set; }
        public Transform CanvasRoot { get; private set; }

        /// <summary> 所有层级的栈管理 </summary>
        private readonly Dictionary<UILayer, UIStack> _layerStacks = new();

        /// <summary> 所有窗口实例栈节点列表（按WindowId索引，每个窗口ID可以对应多个实例） </summary>
        private readonly Dictionary<string, List<UIStackNode>> _windowInstances = new();

        /// <summary> 缓存的UI实例列表（按WindowId索引） </summary>
        private readonly Dictionary<string, List<UIStackNode>> _cachedUI = new();

        /// <summary> 所有窗口实例栈节点（按UniqueId索引） </summary>
        private readonly Dictionary<long, UIStackNode> _allWindows = new();

        /// <summary> UIStackNode对象池 </summary>
        private readonly Stack<UIStackNode> _uiStackNodePool = new();

        /// <summary> 正在进行的异步打开操作列表（按UniqueId索引） </summary>
        private readonly Dictionary<long, Coroutine> _asyncOpenCoroutines = new();

        private readonly Dictionary<string, WindowRegistration> _registry = new();

        private UILayer[] _uiLayers;

        /// <summary> 资源加载器 </summary>
        private IUIResourceLoader _resourceLoader;

        /// <summary> 是否已初始化 </summary>
        private bool _isInitialized = false;

        private List<string> _stringListCache = new();  // 字符串列表缓存

        private readonly List<UIStackNode> _updateSnapshot = new();
        private float _perSecondTimer;
        private float _cacheCleanTimer;
        private const float PER_SECOND_INTERVAL = 1f;
        private const float CACHE_CLEAN_INTERVAL = 5f;

        private long _uniqueIdCounter = 0; // 唯一ID计数器

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary> 设置资源加载器（必须在 Initialize 前调用） </summary>
        public void SetResourceLoader(IUIResourceLoader loader)
        {
            _resourceLoader = loader;
        }

        /// <summary> 初始化UIManager </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            CreatUICamera();
            CreateCanvasRoot();
            InitializeLayers();
            // 只在场景中不存在 EventSystem 时才创建
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                CreateEventSystem();
            }
            ScanAndRegisterAll();
        }

        /// <summary> 创建UI摄像机 </summary>
        private Camera CreatUICamera()
        {
            var cameraGO = new GameObject("UICamera");
            cameraGO.transform.SetParent(transform);
            cameraGO.transform.localPosition = new Vector3(0, 1000, 0);
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Depth;
            camera.cullingMask = LayerMask.GetMask("UI");
            camera.orthographic = true;
            camera.orthographicSize = 10;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.depth = 0;
            UICamera = camera;
            return camera;
        }

        /// <summary> 创建Canvas根节点 </summary>
        private Transform CreateCanvasRoot()
        {
            var canvasRootGO = new GameObject("CanvasRoot");
            canvasRootGO.transform.SetParent(transform);
            canvasRootGO.AddComponent<RectTransform>();
            canvasRootGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            var canvas = canvasRootGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;
            var canvasScaler = canvasRootGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = UIGlobal.CanvasScalerReferenceResolution;
            canvasScaler.screenMatchMode = UIGlobal.CanvasScalerScreenMatchMode;
            canvasScaler.matchWidthOrHeight = UIGlobal.CanvasScalerMatchWidthOrHeight;
            CanvasRoot = canvasRootGO.transform;
            return CanvasRoot;
        }

        /// <summary> 初始化所有层级 </summary>
        private void InitializeLayers()
        {
            var rootSortingLayer = CanvasRoot.GetComponent<Canvas>().sortingLayerID;
            _uiLayers = (UILayer[])Enum.GetValues(typeof(UILayer));
            foreach (UILayer layer in _uiLayers)
            {
                CreateLayer(layer, rootSortingLayer);
            }
        }

        /// <summary> 创建层级 </summary>
        private void CreateLayer(UILayer layer, int rootSortingLayer)
        {
            // 创建Canvas
            GameObject canvasGO = new($"Canvas_{layer}");
            canvasGO.transform.SetParent(CanvasRoot);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.sortingLayerID = rootSortingLayer;
            canvas.sortingOrder = (int)layer * UIGlobal.LayerSortingOrderInterval;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            UIStack stack = new(layer, canvasGO);
            _layerStacks[layer] = stack;
        }

        /// <summary> 创建EventSystem </summary>
        private void CreateEventSystem()
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform);
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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

        /// <summary> 获取指定层级的栈 </summary>
        private bool TryGetUILayerStack(UILayer layer, out UIStack stack)
        {
            return _layerStacks.TryGetValue(layer, out stack);
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
            Close(nodes[0].UniqueId);
        }

        /// <summary> 关闭最新的实例 </summary>
        private void CloseNewestInstance(string windowId)
        {
            var nodes = GetUIStackNodes(windowId);
            if ((nodes == null) || (nodes.Count == 0)) return;
            Close(nodes[^1].UniqueId);
        }

        /// <summary> 处理依赖 </summary>
        private bool HandleDependencies(WindowOpenConfig openConfig)
        {
            if ((openConfig.Dependencies == null) || (openConfig.Dependencies.Count == 0)) return true;
            // 获取缺失的依赖界面ID列表
            _stringListCache.Clear();
            for (int i = 0; i < openConfig.Dependencies.Count; i++)
            {
                if (!ExistUI(openConfig.Dependencies[i]))
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
    }
}
