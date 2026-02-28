using System.Collections.Generic;
using System.Linq;
using PlasticGui.WorkspaceWindow;
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
            node.SetOrder(_stack.Count * UIGlobal.InLayerSortingOrderInterval);
        }

        /// <summary>
        /// 弹出栈
        /// </summary>
        public UIStackNode Pop()
        {
            if (_stack.Count == 0) return null;
            var node = _stack[_stack.Count - 1];
            Remove(node.UniqueId);
            return node;
        }

        /// <summary>
        /// 移除指定节点
        /// </summary>
        public bool Remove(UIStackNode node)
        {
            if (node == null) return false;
            return Remove(node.UniqueId);
        }

        /// <summary>
        /// 移除指定节点
        /// </summary>
        public bool Remove(long uniqueId, bool recaculateSortingOrder = true)
        {
            if (!_nodeDict.TryGetValue(uniqueId, out var node)) return false;
            _stack.Remove(node);
            _nodeDict.Remove(uniqueId);
            if (recaculateSortingOrder)
            {
                RecaculateSortingOrder();
            }
            return true;
        }

        /// <summary>
        /// 插入到指定位置
        /// </summary>
        public void Insert(int index, UIStackNode node)
        {
            if (node == null) return;

            if (index < 0 || index > _stack.Count) index = _stack.Count;
            if (_nodeDict.TryGetValue(node.UniqueId, out var existingNode))
            {
                // 如果已存在，先移除
                Remove(existingNode.UniqueId, false);
            }
            _stack.Insert(index, node);
            _nodeDict[node.UniqueId] = node;
            RecaculateSortingOrder();
        }

        /// <summary>
        /// 获取指定节点
        /// </summary>
        public UIStackNode TryGetNode(long uniqueId, out UIStackNode node)
        {
            return _nodeDict.TryGetValue(uniqueId, out node) ? node : null;
        }

        /// <summary>
        /// 检查节点是否存在
        /// </summary>
        public bool Contains(long uniqueId)
        {
            return _nodeDict.ContainsKey(uniqueId);
        }

        /// <summary>
        /// 获取所有节点（按栈顺序）
        /// </summary>
        public List<UIStackNode> GetAllNodes()
        {
            return new List<UIStackNode>(_stack);
        }

        public void GetAllNodes(List<UIStackNode> result)
        {
            result.Clear();
            result.AddRange(_stack);
        }

        /// <summary>
        /// 获取最上层节点
        /// </summary>
        public UIStackNode GetTopNode(bool skipCoveredCheck = false)
        {
            if (_stack.Count == 0) return null;
            if (!skipCoveredCheck)
            {
                return _stack[^1];
            }
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].OpenConfig.WindowAttr != WindowAttr.SkipCoveredCheck)
                {
                    return _stack[i];
                }
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

        private void RecaculateSortingOrder()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                _stack[i].SetOrder(i * UIGlobal.InLayerSortingOrderInterval);
            }
        }
    }
}
