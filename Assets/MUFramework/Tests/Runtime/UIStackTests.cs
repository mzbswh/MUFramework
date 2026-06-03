using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MUFramework;

namespace MUFramework.Tests
{
    public class UIStackTests
    {
        private UIStack _stack;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("StackRoot");
            _stack = new UIStack(UILayer.Default, _root);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        // ---- helpers ----

        private UIStackNode MakeNode(long id, string windowId = "TestWindow",
            OpenBehavior openBehavior = OpenBehavior.KeepBelow,
            CoveredBehavior whenCovered = CoveredBehavior.Normal,
            WindowAttr attr = WindowAttr.None)
        {
            var config = WindowOpenConfig.Default;
            config.WindowId = windowId;
            config.OpenBehavior = openBehavior;
            config.WhenCovered = whenCovered;
            config.WindowAttr = attr;

            var node = new UIStackNode();
            node.Initialize(config, new TestWindow(), id);

            var go = new GameObject(windowId + "_" + id);
            go.transform.SetParent(_root.transform);
            node.AttachGameObject(go);
            node.Window.Init(node);
            return node;
        }

        // ===== Push =====

        [Test]
        public void Push_EmptyStack_CountIsOne()
        {
            _stack.Push(MakeNode(1));
            Assert.AreEqual(1, _stack.Count);
        }

        [Test]
        public void Push_MultipleNodes_CountIncrements()
        {
            _stack.Push(MakeNode(1));
            _stack.Push(MakeNode(2));
            _stack.Push(MakeNode(3));
            Assert.AreEqual(3, _stack.Count);
        }

        [Test]
        public void Push_NullNode_Ignored()
        {
            _stack.Push(null);
            Assert.AreEqual(0, _stack.Count);
        }

        [Test]
        public void Push_DuplicateNode_MovesToTop()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            _stack.Push(n1); // re-push n1 -> goes to top
            Assert.AreEqual(2, _stack.Count);
            Assert.AreSame(n1, _stack.Top);
        }

        // ===== Pop =====

        [Test]
        public void Pop_ReturnsTopNode()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            var popped = _stack.Pop();
            Assert.AreSame(n2, popped);
        }

        [Test]
        public void Pop_EmptyStack_ReturnsNull()
        {
            Assert.IsNull(_stack.Pop());
        }

        [Test]
        public void Pop_DecreasesCount()
        {
            _stack.Push(MakeNode(1));
            _stack.Push(MakeNode(2));
            _stack.Pop();
            Assert.AreEqual(1, _stack.Count);
        }

        // ===== Remove =====

        [Test]
        public void Remove_ExistingNode_ReturnsTrue()
        {
            var n = MakeNode(1);
            _stack.Push(n);
            Assert.IsTrue(_stack.Remove(n));
        }

        [Test]
        public void Remove_NonExistingNode_ReturnsFalse()
        {
            var n = MakeNode(99);
            Assert.IsFalse(_stack.Remove(n));
        }

        [Test]
        public void Remove_ByUniqueId_DecreasesCount()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            _stack.Remove(1L);
            Assert.AreEqual(1, _stack.Count);
        }

        // ===== Insert =====

        [Test]
        public void Insert_AtIndex0_NodeIsBottom()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Insert(0, n2);
            var all = _stack.GetAllNodes();
            Assert.AreSame(n2, all[0]);
        }

        [Test]
        public void Insert_OutOfRange_ClampedToEnd()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Insert(100, n2);
            Assert.AreSame(n2, _stack.Top);
        }

        [Test]
        public void Insert_NullNode_Ignored()
        {
            _stack.Insert(0, null);
            Assert.AreEqual(0, _stack.Count);
        }

        // ===== Top / IsEmpty =====

        [Test]
        public void Top_EmptyStack_IsNull()
        {
            Assert.IsNull(_stack.Top);
        }

        [Test]
        public void IsEmpty_EmptyStack_IsTrue()
        {
            Assert.IsTrue(_stack.IsEmpty);
        }

        [Test]
        public void IsEmpty_NonEmptyStack_IsFalse()
        {
            _stack.Push(MakeNode(1));
            Assert.IsFalse(_stack.IsEmpty);
        }

        // ===== Contains =====

        [Test]
        public void Contains_ExistingId_ReturnsTrue()
        {
            _stack.Push(MakeNode(5));
            Assert.IsTrue(_stack.Contains(5L));
        }

        [Test]
        public void Contains_MissingId_ReturnsFalse()
        {
            Assert.IsFalse(_stack.Contains(999L));
        }

        // ===== GetTopNode =====

        [Test]
        public void GetTopNode_NoSkip_ReturnsLast()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            Assert.AreSame(n2, _stack.GetTopNode());
        }

        [Test]
        public void GetTopNode_SkipCoveredCheck_SkipsOverlayNodes()
        {
            var n1 = MakeNode(1);
            var nOverlay = MakeNode(2, attr: WindowAttr.SkipCoveredCheck);
            _stack.Push(n1);
            _stack.Push(nOverlay);
            var top = _stack.GetTopNode(skipCoveredCheck: true);
            Assert.AreSame(n1, top);
        }

        [Test]
        public void GetTopNode_SkipBackKey_SkipsBackKeyNodes()
        {
            var n1 = MakeNode(1);
            var nSkip = MakeNode(2, attr: WindowAttr.SkipBackKey);
            _stack.Push(n1);
            _stack.Push(nSkip);
            var top = _stack.GetTopNode(skipBackKeyCheck: true);
            Assert.AreSame(n1, top);
        }

        // ===== Clear =====

        [Test]
        public void Clear_EmptiesStack()
        {
            _stack.Push(MakeNode(1));
            _stack.Push(MakeNode(2));
            _stack.Clear();
            Assert.AreEqual(0, _stack.Count);
            Assert.IsTrue(_stack.IsEmpty);
        }

        // ===== GetAllNodes =====

        [Test]
        public void GetAllNodes_ReturnsInPushOrder()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            var n3 = MakeNode(3);
            _stack.Push(n1);
            _stack.Push(n2);
            _stack.Push(n3);
            var all = _stack.GetAllNodes();
            Assert.AreEqual(3, all.Count);
            Assert.AreSame(n1, all[0]);
            Assert.AreSame(n3, all[2]);
        }

        [Test]
        public void GetAllNodes_List_FillsResult()
        {
            _stack.Push(MakeNode(1));
            _stack.Push(MakeNode(2));
            var result = new List<UIStackNode>();
            _stack.GetAllNodes(result);
            Assert.AreEqual(2, result.Count);
        }

        // ===== UpdateAllStackNode (覆盖状态传播) =====

        [Test]
        public void UpdateAllStackNode_TopNodeNotCovered()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            // 栈顶节点（n2）应当不被覆盖
            Assert.IsFalse(n2.IsCovered);
        }

        [Test]
        public void UpdateAllStackNode_SecondNodeIsCovered()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            // n1 在 n2 下方，应被覆盖
            Assert.IsTrue(n1.IsCovered);
        }

        [Test]
        public void UpdateAllStackNode_HideBehavior_HidesLowerNodes()
        {
            // 栈顶 OpenBehavior=HideBelow，下方节点应被隐藏
            var nBottom = MakeNode(1, openBehavior: OpenBehavior.KeepBelow, whenCovered: CoveredBehavior.Hide);
            var nTop = MakeNode(2, openBehavior: OpenBehavior.HideBelow);
            _stack.Push(nBottom);
            _stack.Push(nTop);
            Assert.IsTrue(nBottom.IsHidden);
            Assert.IsTrue(nBottom.IsPause);
        }

        [Test]
        public void UpdateAllStackNode_PauseBehavior_PausesLowerNodes()
        {
            var nBottom = MakeNode(1, whenCovered: CoveredBehavior.Pause);
            var nTop = MakeNode(2, openBehavior: OpenBehavior.PauseBelow);
            _stack.Push(nBottom);
            _stack.Push(nTop);
            Assert.IsTrue(nBottom.IsPause);
            Assert.IsFalse(nBottom.IsHidden);
        }

        [Test]
        public void UpdateAllStackNode_AfterRemove_RestoresCoveredState()
        {
            var nBottom = MakeNode(1, whenCovered: CoveredBehavior.Hide);
            var nTop = MakeNode(2, openBehavior: OpenBehavior.HideBelow);
            _stack.Push(nBottom);
            _stack.Push(nTop);
            Assert.IsTrue(nBottom.IsHidden);

            _stack.Remove(nTop);
            // nBottom 现在是栈顶，不再被覆盖
            Assert.IsFalse(nBottom.IsCovered);
        }

        [Test]
        public void UpdateAllStackNode_SkipCoveredCheck_NodeNotAffected()
        {
            var nBase = MakeNode(1);
            var nOverlay = MakeNode(2, attr: WindowAttr.SkipCoveredCheck);
            var nTop = MakeNode(3, openBehavior: OpenBehavior.HideBelow);
            _stack.Push(nBase);
            _stack.Push(nOverlay);
            _stack.Push(nTop);
            // nOverlay 有 SkipCoveredCheck，不参与覆盖判断
            Assert.IsFalse(nOverlay.IsCovered);
            Assert.IsFalse(nOverlay.IsHidden);
        }

        // ===== SortingOrder 重新计算 =====

        [Test]
        public void RecalculateSortingOrder_AfterPush_OrdersAscending()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            _stack.Push(n1);
            _stack.Push(n2);
            Assert.Less(n1.Canvas.sortingOrder, n2.Canvas.sortingOrder);
        }

        [Test]
        public void RecalculateSortingOrder_AfterRemove_Recalculated()
        {
            var n1 = MakeNode(1);
            var n2 = MakeNode(2);
            var n3 = MakeNode(3);
            _stack.Push(n1);
            _stack.Push(n2);
            _stack.Push(n3);
            int oldOrder = n3.Canvas.sortingOrder;
            _stack.Remove(n2);
            // n3 现在变为索引 1（0-based），sortingOrder 应小于之前
            Assert.Less(n3.Canvas.sortingOrder, oldOrder);
        }
    }
}
