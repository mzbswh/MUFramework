using System.Collections.Generic;
using System.Linq;

namespace MUFramework
{
    /// <summary>
    /// UI栈管理类
    /// 每个UILayer对应一个UIStack实例
    /// </summary>
    public class UIStack
    {
        /// <summary>
        /// 栈节点列表（维护顺序）
        /// </summary>
        private List<StackNode> _stack = new List<StackNode>();

        /// <summary>
        /// 栈节点字典（维护快速查找）
        /// </summary>
        private Dictionary<string, StackNode> _nodeDict = new Dictionary<string, StackNode>();

        /// <summary>
        /// 层级配置
        /// </summary>
        public LayerConfig LayerConfig { get; set; }

        /// <summary>
        /// 栈中节点数量
        /// </summary>
        public int Count => _stack.Count;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => _stack.Count == 0;

        /// <summary>
        /// 获取栈顶节点
        /// </summary>
        public StackNode Top
        {
            get
            {
                if (_stack.Count > 0)
                    return _stack[_stack.Count - 1];
                return null;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIStack(LayerConfig layerConfig = null)
        {
            LayerConfig = layerConfig;
        }

        /// <summary>
        /// 推入栈
        /// </summary>
        public void Push(StackNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.UniqueId))
                return;

            if (_nodeDict.ContainsKey(node.UniqueId))
            {
                // 如果已存在，先移除
                Remove(node.UniqueId);
            }

            _stack.Add(node);
            _nodeDict[node.UniqueId] = node;
        }

        /// <summary>
        /// 弹出栈
        /// </summary>
        public StackNode Pop()
        {
            if (_stack.Count == 0)
                return null;

            var node = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _nodeDict.Remove(node.UniqueId);
            return node;
        }

        /// <summary>
        /// 移除指定节点
        /// </summary>
        public bool Remove(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId) || !_nodeDict.ContainsKey(uniqueId))
                return false;

            var node = _nodeDict[uniqueId];
            _stack.Remove(node);
            _nodeDict.Remove(uniqueId);
            return true;
        }

        /// <summary>
        /// 移除指定节点
        /// </summary>
        public bool Remove(StackNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.UniqueId))
                return false;

            return Remove(node.UniqueId);
        }

        /// <summary>
        /// 插入到指定位置
        /// </summary>
        public void Insert(int index, StackNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.UniqueId))
                return;

            if (index < 0 || index > _stack.Count)
                index = _stack.Count;

            if (_nodeDict.ContainsKey(node.UniqueId))
            {
                // 如果已存在，先移除
                Remove(node.UniqueId);
            }

            _stack.Insert(index, node);
            _nodeDict[node.UniqueId] = node;
        }

        /// <summary>
        /// 获取指定节点
        /// </summary>
        public StackNode Get(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return null;

            _nodeDict.TryGetValue(uniqueId, out var node);
            return node;
        }

        /// <summary>
        /// 检查节点是否存在
        /// </summary>
        public bool Contains(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return false;

            return _nodeDict.ContainsKey(uniqueId);
        }

        /// <summary>
        /// 获取所有节点（按栈顺序）
        /// </summary>
        public List<StackNode> GetAllNodes()
        {
            return new List<StackNode>(_stack);
        }

        /// <summary>
        /// 获取指定类型的最上层节点（跳过Overlay类型）
        /// </summary>
        public StackNode GetTopNormalNode()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].Type == WindowType.Normal)
                    return _stack[i];
            }
            return null;
        }

        /// <summary>
        /// 清空栈
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
            _nodeDict.Clear();
        }

        /// <summary>
        /// 获取指定节点的索引
        /// </summary>
        public int IndexOf(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return -1;

            var node = Get(uniqueId);
            if (node == null)
                return -1;

            return _stack.IndexOf(node);
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public StackNode GetAt(int index)
        {
            if (index < 0 || index >= _stack.Count)
                return null;

            return _stack[index];
        }
    }
}
