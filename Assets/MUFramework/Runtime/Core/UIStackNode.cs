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
        public double ExpireTime { get; private set; }

        /// <summary> 动画辅助器 </summary>
        public IUIAnimation UIAnimation { get; private set; }

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

        /// <summary> 是否正在加载中 </summary>
        public bool IsLoading => State.HasState(UIState.Loading);

        /// <summary> 是否正在加载完成了 </summary>
        public bool IsLoaded => State.HasState(UIState.Loaded);

        public UILayer Layer => OpenConfig.Layer;
        public CacheType CacheType => OpenConfig.CacheType;
        public WindowAttr WindowAttr => OpenConfig.WindowAttr;

        public void Initialize(WindowOpenConfig openConfig, UIWindow window, long uniqueId)
        {
            UniqueId = uniqueId;
            WindowId = openConfig.WindowId;
            Window = window;
            OpenConfig = openConfig;
            State = UIState.None;
            UIAnimation = openConfig.UIAnimation ?? UIGlobal.DefaultAnimation;
            ExpireTime = -1;
            SetState(UIState.Loading);
        }

        public void SetUniqueId(long uniqueId)
        {
            UniqueId = uniqueId;
        }

        public void AttackGameObject(GameObject obj)
        {
            GameObject = obj;
            Transform = obj.transform;
            CanvasGroup = obj.GetOrAddComponent<CanvasGroup>();
            Canvas = obj.GetOrAddComponent<Canvas>();
            UnsetState(UIState.Loading);
            SetState(UIState.Loaded);
        }

        public void SetState(UIState state)
        {
            State |= state;
        }

        public void UnsetState(UIState state)
        {
            State &= ~state;
        }

        public void SetClosedState()
        {
            SetState(UIState.Closed | UIState.Paused | UIState.Hidden);
            UnsetState(UIState.Closing);
        }

        public void SetCover(bool cover)
        {
            if (IsCovered == cover) return;
            if (cover)
            {
                SetState(UIState.Covered);
                if (IsLoaded)
                {
                    Window.OnCovere();
                }
            }
            else
            {
                UnsetState(UIState.Covered);
                if (IsLoaded)
                {
                    Window.OnUncover();
                }
            }
        }

        public void SetHide(bool hide)
        {
            if (IsHidden == hide) return;
            if (hide)
            {
                SetState(UIState.Hidden);
                if (IsLoaded)
                {
                    Window.Hide();
                }
            }
            else
            {
                UnsetState(UIState.Hidden);
                if (IsLoaded)
                {
                    Window.Show();
                }
            }
        }

        public void SetPause(bool pause)
        {
            if (IsPause == pause) return;
            if (pause)
            {
                SetState(UIState.Paused);
                if (IsLoaded)
                {
                    Window.Pause();
                }
            }
            else
            {
                UnsetState(UIState.Paused);
                if (IsLoaded)
                {
                    Window.Resume();
                }
            }
        }

        public void SetExpireTime(double expireTime)
        {
            ExpireTime = expireTime;
        }

        public void SetOrder(int order)
        {
            Canvas.sortingOrder = order;
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
            State = UIState.None;
            GameObject = null;
            Transform = null;
            CanvasGroup = null;
            Canvas = null;
            UIAnimation = null;
            ExpireTime = -1;
        }
    }
}
