using System.Collections.Generic;
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
        public GameObject Root { get; private set; }

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
            UpdateAllStackNode();
        }

        /// <summary>
        /// 弹出栈
        /// </summary>
        public UIStackNode Pop()
        {
            if (_stack.Count == 0) return null;
            var node = _stack[_stack.Count - 1];
            Remove(node.UniqueId, false);
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
        public bool Remove(long uniqueId, bool recalculate = true)
        {
            if (!_nodeDict.TryGetValue(uniqueId, out var node)) return false;
            _stack.Remove(node);
            _nodeDict.Remove(uniqueId);
            if (recalculate)
            {
                RecalculateSortingOrder();
                UpdateAllStackNode();
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
            RecalculateSortingOrder();
            UpdateAllStackNode();
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
        public IReadOnlyList<UIStackNode> GetAllNodes()
        {
            return _stack;
        }

        public void GetAllNodes(List<UIStackNode> result)
        {
            result.Clear();
            result.AddRange(_stack);
        }

        /// <summary>
        /// 获取最上层节点
        /// </summary>
        public UIStackNode GetTopNode(bool skipCoveredCheck = false, bool skipBackKeyCheck = false)
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var node = _stack[i];
                if (node.IsClosing || node.IsClosed) continue;
                if (skipCoveredCheck && node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipCoveredCheck)) continue;
                if (skipBackKeyCheck && node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipBackKey)) continue;
                return node;
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

        private void RecalculateSortingOrder()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                _stack[i].SetOrder(i * UIGlobal.InLayerSortingOrderInterval);
            }
        }

        private void UpdateAllStackNode()
        {
            int coverLevel = 0; // 0=Keep, 1=Pause, 2=Hide
            bool covered = false;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var node = _stack[i];
                if (node.IsClosing || node.IsClosed) continue;
                if (node.OpenConfig.WindowAttr.HasFlag(WindowAttr.SkipCoveredCheck)) continue;

                node.SetCover(covered);
                // 取上方累积的 coverLevel 与自身 WhenCovered 的最大值
                CoveredBehavior finalBehavior = (int)node.OpenConfig.WhenCovered > coverLevel
                    ? node.OpenConfig.WhenCovered
                    : (CoveredBehavior)coverLevel;
                switch (finalBehavior)
                {
                    case CoveredBehavior.Normal:
                    {
                        node.SetHide(false);
                        node.SetPause(false);
                        break;
                    }
                    case CoveredBehavior.Pause:
                    {
                        node.SetHide(false);
                        node.SetPause(true);
                        break;
                    }
                    case CoveredBehavior.Hide:
                    {
                        node.SetPause(true);
                        node.SetHide(true);
                        break;
                    }
                }
                // 累积当前节点的 OpenBehavior，影响更下方节点
                if ((int)node.OpenConfig.OpenBehavior > coverLevel)
                {
                    coverLevel = (int)node.OpenConfig.OpenBehavior;
                }
                covered = true; // 第一个节点以下的所有节点都处于被覆盖状态
            }
        }
    }
}
