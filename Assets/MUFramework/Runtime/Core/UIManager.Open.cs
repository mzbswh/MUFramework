using System;
using System.Collections;
using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        public TWindow Open<TWindow>() where TWindow : UIWindow
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null) return null;
            return Open(config) as TWindow;
        }

        public TWindow Open<TWindow, TData>(TData data, Action<WindowOpenConfig> configOverride)
            where TWindow : UIWindow<TData>
        {
            var config = ResolveConfig(typeof(TWindow).Name, configOverride);
            if (config == null) return null;
            return Open(config, data) as TWindow;
        }

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

        public TWindow Open<TWindow>(WindowOpenConfig openConfig, params object[] args) where TWindow : UIWindow
        {
            var window = Open(openConfig, args);
            return window as TWindow;
        }

        public TWindow Open<TWindow, TArgs1>(WindowOpenConfig openConfig, TArgs1 arg1) where TWindow : UIWindow, IOpenArgs<TArgs1>
            => Open(openConfig, arg1) as TWindow;

        public TWindow Open<TWindow, TArgs1, TArgs2>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2>
            => Open(openConfig, arg1, arg2) as TWindow;

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3>
            => Open(openConfig, arg1, arg2, arg3) as TWindow;

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3, TArgs4>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4>
            => Open(openConfig, arg1, arg2, arg3, arg4) as TWindow;

        public TWindow Open<TWindow, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(WindowOpenConfig openConfig, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4, TArgs5 arg5) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
            => Open(openConfig, arg1, arg2, arg3, arg4, arg5) as TWindow;

        public UIWindow Open(WindowOpenConfig openConfig, params object[] args)
        {
            if (!PreCheckMultiInstance(openConfig)) return null;
            if (!HandleDependencies(openConfig)) return null;
            if (!HandleMultiInstance(openConfig)) return null;

            UIStackNode node = TryGetCachedUI(openConfig.WindowId);
            if (node == null)
            {
                GameObject gameObject = _resourceLoader?.LoadGameObject(openConfig.WindowId);
                if (gameObject == null)
                {
                    UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"Failed to load window resource: {openConfig.WindowId}");
                    return null;
                }
                node = CreateUIStackNode(openConfig);
                node.AttachGameObject(gameObject);
            }
            node.SetUniqueId(GenerateUniqueId());
            AddUIStackNode(node);
            return OpenCore(node, args);
        }

        public long OpenAsync<TWindow>(Action<TWindow> callback) where TWindow : UIWindow
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null)
            {
                callback?.Invoke(null);
                return -1;
            }
            return OpenAsync(config, window => callback?.Invoke(window as TWindow));
        }

        public long OpenAsync<TWindow, T1>(T1 arg1, Action<TWindow> callback)
            where TWindow : UIWindow, IOpenArgs<T1>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null)
            {
                callback?.Invoke(null);
                return -1;
            }
            return OpenAsync(config, window => callback?.Invoke(window as TWindow), arg1);
        }

        public long OpenAsync<TWindow, T1, T2>(T1 arg1, T2 arg2, Action<TWindow> callback)
            where TWindow : UIWindow, IOpenArgs<T1, T2>
        {
            var config = ResolveConfig(typeof(TWindow).Name);
            if (config == null)
            {
                callback?.Invoke(null);
                return -1;
            }
            return OpenAsync(config, window => callback?.Invoke(window as TWindow), arg1, arg2);
        }

        public long OpenAsync(WindowOpenConfig openConfig, Action<UIWindow> callback, params object[] args)
        {
            var uniqueId = GenerateUniqueId();
            _asyncOpenCallbacks[uniqueId] = callback;
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, window =>
            {
                CompleteAsyncOpen(uniqueId, window);
            }, args));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow>(WindowOpenConfig openConfig, Action<TWindow> callback, params object[] args) where TWindow : UIWindow
        {
            var uniqueId = GenerateUniqueId();
            _asyncOpenCallbacks[uniqueId] = window => callback?.Invoke(window as TWindow);
            var coroutine = StartCoroutine(OpenAsyncCoroutine(openConfig, uniqueId, window =>
            {
                CompleteAsyncOpen(uniqueId, window);
            }, args));
            _asyncOpenCoroutines[uniqueId] = coroutine;
            return uniqueId;
        }

        public long OpenAsync<TWindow, TArgs1>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1) where TWindow : UIWindow, IOpenArgs<TArgs1>
            => OpenAsync(openConfig, window => callback?.Invoke(window as TWindow), arg1);

        public long OpenAsync<TWindow, TArgs1, TArgs2>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2>
            => OpenAsync(openConfig, window => callback?.Invoke(window as TWindow), arg1, arg2);

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3>
            => OpenAsync(openConfig, window => callback?.Invoke(window as TWindow), arg1, arg2, arg3);

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3, TArgs4>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4>
            => OpenAsync(openConfig, window => callback?.Invoke(window as TWindow), arg1, arg2, arg3, arg4);

        public long OpenAsync<TWindow, TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>(WindowOpenConfig openConfig, Action<TWindow> callback, TArgs1 arg1, TArgs2 arg2, TArgs3 arg3, TArgs4 arg4, TArgs5 arg5) where TWindow : UIWindow, IOpenArgs<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>
            => OpenAsync(openConfig, window => callback?.Invoke(window as TWindow), arg1, arg2, arg3, arg4, arg5);

        private IEnumerator OpenAsyncCoroutine(WindowOpenConfig openConfig, long uniqueId, Action<UIWindow> callback, params object[] args)
        {
            if (!PreCheckMultiInstance(openConfig) ||
                !HandleDependencies(openConfig) ||
                !HandleMultiInstance(openConfig))
            {
                callback?.Invoke(null);
                yield break;
            }

            UIStackNode node = TryGetCachedUI(openConfig.WindowId);
            if (node != null)
            {
                node.SetUniqueId(uniqueId);
                AddUIStackNode(node);
                yield return null;
                callback?.Invoke(OpenCore(node, args));
                yield break;
            }

            node = CreateUIStackNode(openConfig, uniqueId);
            AddUIStackNode(node);
            GameObject obj = null;
            if (_resourceLoader != null)
            {
                var loadRoutine = _resourceLoader.LoadGameObjectAsync(openConfig.WindowId, go => obj = go);
                while (loadRoutine != null && loadRoutine.MoveNext())
                {
                    yield return loadRoutine.Current;
                }
            }
            if (!ExistUI(uniqueId))
            {
                callback?.Invoke(null);
                yield break;
            }
            if (obj == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"Failed to load window resource async: {openConfig.WindowId}");
                callback?.Invoke(null);
                yield break;
            }
            node.AttachGameObject(obj);
            callback?.Invoke(OpenCore(node, args));
        }

        private UIWindow OpenCore(UIStackNode node, params object[] args)
        {
            if (!_layerStacks.TryGetValue(node.OpenConfig.Layer, out var stack))
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI], OpenCore failed, target layer {node.OpenConfig.Layer} stack not found");
                return null;
            }

            var obj = node.GameObject;
            obj.transform.SetParent(stack.Root.transform, false);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.transform.localScale = Vector3.one;

            node.Window.Init(node);
            node.Window.OnOpenInternal(args ?? Array.Empty<object>());
            SafeAreaAdapter.AdaptSafeArea(node.GameObject);
            UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_OPEN, node.WindowId);
            if (!node.IsHidden)
            {
                node.Window.Show();
                UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_SHOW, node.WindowId);
            }
            if (node.IsPause)
            {
                node.Window.OnPauseInternal(true);
            }
            if (node.IsCovered)
            {
                node.Window.OnCoverInternal(true);
            }
            return node.Window;
        }

        private void CompleteAsyncOpen(long uniqueId, UIWindow window)
        {
            _asyncOpenCoroutines.Remove(uniqueId);
            if (!_asyncOpenCallbacks.TryGetValue(uniqueId, out var callback)) return;
            _asyncOpenCallbacks.Remove(uniqueId);
            callback?.Invoke(window);
        }
    }
}
