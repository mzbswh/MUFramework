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
            {
                if (_cachedUI[key].Count == 0)
                {
                    _stringListCache.Add(key);
                }
            }
            for (int i = 0; i < _stringListCache.Count; i++)
            {
                _cachedUI.Remove(_stringListCache[i]);
            }
        }
    }
}
