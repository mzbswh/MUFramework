using System;

namespace MUFramework
{
    public partial class UIManager
    {
        public void Close<TWindow>(bool withAnimation = true) where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return;
            Close(nodes[0].UniqueId, withAnimation);
        }

        public void CloseAll<TWindow>(bool withAnimation = true) where TWindow : UIWindow
        {
            Close(typeof(TWindow).Name, closeAllInstances: true, withAnimation: withAnimation);
        }

        public void CloseAll(bool withAnimation = true)
        {
            _stringListCache.Clear();
            foreach (var key in _windowInstances.Keys)
            {
                _stringListCache.Add(key);
            }
            for (int i = 0; i < _stringListCache.Count; i++)
            {
                Close(_stringListCache[i], closeAllInstances: true, withAnimation: withAnimation);
            }
        }

        public bool CancelAsync(long uniqueId)
        {
            if (!_asyncOpenCoroutines.TryGetValue(uniqueId, out var coroutine)) return false;
            StopCoroutine(coroutine);
            _asyncOpenCoroutines.Remove(uniqueId);
            RemoveUIStackNode(uniqueId);
            if (_asyncOpenCallbacks.TryGetValue(uniqueId, out var callback))
            {
                _asyncOpenCallbacks.Remove(uniqueId);
                callback?.Invoke(null);
            }
            return true;
        }

        public void Close(string windowId, bool closeAllInstances = true, bool withAnimation = true, Action onComplete = null)
        {
            var instances = GetUIStackNodes(windowId);
            if (instances == null || instances.Count == 0) return;
            if (closeAllInstances)
            {
                var ids = new long[instances.Count];
                for (int i = 0; i < instances.Count; i++) ids[i] = instances[i].UniqueId;
                for (int i = 0; i < ids.Length; i++) Close(ids[i], withAnimation, onComplete);
            }
            else
            {
                Close(instances[0].UniqueId, withAnimation, onComplete);
            }
        }

        public void Close(long uniqueId, bool withAnimation = true, Action onComplete = null)
        {
            if (CancelAsync(uniqueId))
            {
                return;
            }
            var node = GetUIStackNode(uniqueId);
            if (node == null) return;
            if (node.IsClosing || node.IsClosed) return;
            node.SetState(UIState.Closing);
            node.Window.SetInteractable(false);
            node.SetPause(true);
            UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_PAUSE, node.WindowId);
            node.Window.Hide(withAnimation, () =>
            {
                node.SetClosedState();
                node.Window.CompleteClose();
                UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_CLOSE, node.WindowId);
                onComplete?.Invoke();
                switch (node.CacheType)
                {
                    case CacheType.Persistent:
                        AddUIToCache(node);
                        break;
                    case CacheType.ExpireTime:
                        AddUIToCache(node);
                        node.SetExpireTime(UnityEngine.Time.unscaledTimeAsDouble + node.OpenConfig.ExpireTime);
                        break;
                    default:
                        RemoveUIStackNode(node);
                        break;
                }
            });
        }
    }
}
