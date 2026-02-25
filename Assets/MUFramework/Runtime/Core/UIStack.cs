using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private List<UIStackNode> _stack = new List<UIStackNode>();

        /// <summary>
        /// 栈节点字典（维护快速查找）
        /// </summary>
        private Dictionary<long, UIStackNode> _nodeDict = new Dictionary<long, UIStackNode>();

        /// <summary> 层级 </summary>
        public UILayer Layer { get; private set; }

        /// <summary> 根GameObject </summary>
        public GameObject Root { get; set; }

        /// <summary> 栈中节点数量 </summary>
        public int Count => _stack.Count;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => _stack.Count == 0;

        /// <summary>
        /// 获取栈顶节点
        /// </summary>
        public UIStackNode Top
        {
            get
            {
                if (_stack.Count > 0) return _stack[^1];
                return null;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIStack(UILayer layer, GameObject root)
        {
            Layer = layer;
            Root = root;
        }

        /// <summary>
        /// 推入栈
        /// </summary>
        public void Push(UIStackNode node)
        {
            if (node == null) return;

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
        public UIStackNode Pop()
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
        public bool Remove(UIStackNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.UniqueId))
                return false;

            return Remove(node.UniqueId);
        }

        /// <summary>
        /// 插入到指定位置
        /// </summary>
        public void Insert(int index, UIStackNode node)
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
        public UIStackNode Get(string uniqueId)
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
        public List<UIStackNode> GetAllNodes()
        {
            return new List<UIStackNode>(_stack);
        }

        /// <summary>
        /// 获取指定类型的最上层节点（跳过Overlay类型）
        /// </summary>
        public UIStackNode GetTopNormalNode()
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
        public UIStackNode GetAt(int index)
        {
            if (index < 0 || index >= _stack.Count)
                return null;

            return _stack[index];
        }
    }
}
