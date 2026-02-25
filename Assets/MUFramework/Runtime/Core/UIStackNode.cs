using System;
using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// 栈节点数据结构
    /// </summary>
    public class UIStackNode : IDisposable
    {
        /// <summary> 唯一标识 </summary>
        public long UniqueId { get; private set; }

        /// <summary> 窗口ID </summary>
        public string WindowId { get; private set; }

        public WindowOpenConfig OpenConfig { get; private set; }

        /// <summary> 窗口实例 </summary>
        public UIWindow Window { get; private set; }

        /// <summary> 当前状态 </summary>
        public UIState State { get; private set; }

        /// <summary> 根GameObject </summary>
        public GameObject GameObject { get; private set; }

        /// <summary> 根Transform </summary>
        public Transform Transform { get; private set; }

        /// <summary> CanvasGroup组件 </summary>
        public CanvasGroup CanvasGroup { get; private set; }

        /// <summary> Canvas组件 </summary>
        public Canvas Canvas { get; private set; }

        /// <summary> 过期时间(单位：秒) </summary>
        public long ExpireTime { get; private set; }

        /// <summary> 动画辅助器 </summary>
        public IUIAnimation AnimationHelper { get; private set; }

        /// <summary> 是否暂停中 </summary>
        public bool IsPause => State.HasState(UIState.Paused);

        /// <summary> 是否被覆盖 </summary>
        public bool IsCovered => State.HasState(UIState.Covered);

        /// <summary> 是否隐藏中 </summary>
        public bool IsHidden => State.HasState(UIState.Hidden);

        /// <summary> 是否正在关闭 </summary>
        public bool IsClosing => State.HasState(UIState.Closing);

        /// <summary> 是否已关闭 </summary>
        public bool IsClosed => State.HasState(UIState.Closed);

        public void SetState(UIState state)
        {
            State |= state;
        }

        public void UnsetState(UIState state)
        {
            State &= ~state;
        }

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            UniqueId = 0;
            WindowId = null;
            Window = null;
            State = UIState.Closed;
            GameObject = null;
            Transform = null;
            CanvasGroup = null;
            Canvas = null;
            AnimationHelper = null;
            ExpireTime = -1;
        }
    }
}
