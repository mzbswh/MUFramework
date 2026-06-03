using System.Collections.Generic;

namespace MUFramework
{
    public partial class UIManager
    {
        public TWindow GetWindow<TWindow>() where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return null;
            return nodes[0].Window as TWindow;
        }

        public bool TryGetWindow<TWindow>(out TWindow window) where TWindow : UIWindow
        {
            window = GetWindow<TWindow>();
            return window != null;
        }

        public IReadOnlyList<TWindow> GetWindows<TWindow>() where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null || nodes.Count == 0) return System.Array.Empty<TWindow>();
            var result = new List<TWindow>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Window is TWindow window) result.Add(window);
            }
            return result;
        }

        public bool IsOpen<TWindow>() where TWindow : UIWindow
            => ExistUI(typeof(TWindow).Name);

        public bool ExistUI(long uniqueId)
            => GetUIStackNode(uniqueId) != null;

        public bool ExistUI(string windowId)
        {
            var nodes = GetUIStackNodes(windowId);
            return nodes != null && nodes.Count > 0;
        }

        internal void PauseWindow(long uniqueId)
        {
            var node = GetUIStackNode(uniqueId);
            node?.SetPause(true);
            if (node != null)
            {
                UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_PAUSE, node.WindowId);
            }
        }

        internal void ResumeWindow(long uniqueId)
        {
            var node = GetUIStackNode(uniqueId);
            node?.SetPause(false);
            if (node != null)
            {
                UIGlobal.FireEvent(UIGlobal.UI_EVENT_WINDOW_RESUME, node.WindowId);
            }
        }
    }
}
