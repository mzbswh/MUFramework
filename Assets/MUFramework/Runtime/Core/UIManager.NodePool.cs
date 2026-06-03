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
            var registration = GetRegistration(windowId);
            Type type = registration?.WindowType ?? UIGlobal.GetWindowClassTypeFunc?.Invoke(windowId);
            if (type == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI] Cannot resolve window type for '{windowId}'.");
                return null;
            }
            if (type.IsAbstract || !typeof(UIWindow).IsAssignableFrom(type))
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI] Invalid window type '{type}' for '{windowId}'.");
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
            if (go != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(go);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _uiStackNodePool.Push(node);
        }
    }
}
