namespace MUFramework
{
    public partial class UIManager
    {
        public void SendMessage<TWindow>(string msg, params object[] args) where TWindow : UIWindow
        {
            var nodes = GetUIStackNodes(typeof(TWindow).Name);
            if (nodes == null) return;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].IsLoaded)
                {
                    nodes[i].Window.OnMessageInternal(msg, args);
                }
            }
        }

        public void SendMessage(long uniqueId, string msg, params object[] args)
        {
            var node = GetUIStackNode(uniqueId);
            if (node != null && node.IsLoaded)
            {
                node.Window.OnMessageInternal(msg, args);
            }
        }

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
