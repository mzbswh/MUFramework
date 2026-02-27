using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace MUFramework
{
    /// <summary>
    /// UIWindow基类
    /// 一个window代表一个完整的UI界面
    /// </summary>
    public abstract class UIWindow
    {
        /// <summary> 唯一ID </summary>
        public long UniqueId => _stackNode.UniqueId;

        /// <summary> 根GameObject </summary>
        public GameObject GameObject => _stackNode.GameObject;

        /// <summary> 根Transform </summary>
        public Transform Transform => _stackNode.Transform;

        /// <summary> CanvasGroup组件 </summary>
        public CanvasGroup CanvasGroup => _stackNode.CanvasGroup;

        /// <summary> Canvas组件 </summary>
        public Canvas Canvas => _stackNode.Canvas;

        /// <summary> 动画 </summary>
        public IUIAnimation UIAnimation => _stackNode.UIAnimation;

        /// <summary> 是否暂停中 </summary>
        public bool IsPause => _stackNode.IsPause;

        /// <summary> 是否被覆盖 </summary>
        public bool IsCovered => _stackNode.IsCovered;

        /// <summary> 是否隐藏中 </summary>
        public bool IsHidden => _stackNode.IsHidden;

        /// <summary> 是否正在关闭 </summary>
        public bool IsClosing => _stackNode.IsClosing;

        /// <summary> 是否已关闭 </summary>
        public bool IsClosed => _stackNode.IsClosed;

        /// <summary> UIWidgets </summary>
        private readonly List<UIWidget> _widgets = new();

        private UIStackNode _stackNode;

        public void Init(UIStackNode stackNode)
        {
            _stackNode = stackNode;
            OnCreate();
        }

        public void Open(params object[] args)
        {
            OnOpen(args);
        }

        public void Show(bool withAnimation = false, Action onComplete = null)
        {
            OnBeforeShow();
            SetInteractable(false);
            if (withAnimation && (UIAnimation != null))
            {
                UIAnimation.PlayOpen(GameObject, OnPlayEnd);
            }
            else
            {
                OnPlayEnd();
            }

            void OnPlayEnd()
            {
                SetInteractable(true);
                GameObject.SetActive(true);
                OnShow();
                onComplete?.Invoke();
            }
        }

        public void Resume()
        {
            if (!IsPause) return;
            OnResume();
        }

        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].Update(deltaTime);
            }
        }

        public void UpdatePerSecond()
        {
            OnUpdatePerSecond();
        }

        public void Pause()
        {
            if (IsPause) return;
            OnPause();
        }

        public void Hide(bool withAnimation = true, Action onComplete = null)
        {
            OnBeforeHide();
            SetInteractable(false);
            if (withAnimation && (UIAnimation != null))
            {
                UIAnimation.PlayClose(GameObject, OnPlayEnd);
            }
            else
            {
                OnPlayEnd();
            }

            void OnPlayEnd()
            {
                GameObject.SetActive(false);
                OnHide();
                onComplete?.Invoke();
            }
        }

        public void Destroy()
        {
            OnDestroy();
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].Destroy();
            }
            _widgets.Clear();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void Close(bool withAnimation = true, Action onComplete = null)
        {
            UIManager.Instance.Close(UniqueId, withAnimation, onComplete);
        }

        public void SetInteractable(bool interactable)
        {
            if (CanvasGroup == null) return;
            CanvasGroup.interactable = interactable;
            CanvasGroup.blocksRaycasts = interactable;
        }

        /// <summary> 完成关闭：仅内部UIManager调用 </summary>
        internal void CompleteClose()
        {
            OnClose();
        }

        // ===== 附加的Widgets =====

        public void AttachWidget(UIWidget widget)
        {
            if (_widgets.Contains(widget)) return;
            _widgets.Add(widget);
            widget.AttachTo(this);
        }

        // ===== 子类重写生命周期回调 =====

        /// <summary> 创建时调用 </summary>
        protected virtual void OnCreate() { }
        /// <summary> 打开时调用 </summary>
        protected virtual void OnOpen(params object[] args) { }
        /// <summary> 显示前调用(如果有动画，则动画播放前调用) </summary>
        protected virtual void OnBeforeShow() { }
        /// <summary> 显示时调用(如果有动画，则动画播放完成后调用) </summary>
        protected virtual void OnShow() { }
        /// <summary> 恢复时调用 </summary>
        protected virtual void OnResume() { }
        /// <summary> 未被覆盖时调用 </summary>
        protected virtual void OnUncover() { }
        /// <summary> 更新时调用 </summary>
        protected virtual void OnUpdate(float deltaTime) { }
        /// <summary> 每秒更新时调用(通用逻辑) </summary>
        protected virtual void OnUpdatePerSecond() { }
        /// <summary> 被覆盖时调用 </summary>
        protected virtual void OnCovered() { }
        /// <summary> 暂停时调用 </summary>
        protected virtual void OnPause() { }
        /// <summary> 隐藏前调用(如果有动画，则动画播放前调用) </summary>
        protected virtual void OnBeforeHide() { }
        /// <summary> 隐藏时调用 </summary>
        protected virtual void OnHide() { }
        /// <summary> 关闭时调用(仅内部调用，外部关闭界面统一使用UIManager.Instance.Close) </summary>
        protected virtual void OnClose() { }
        /// <summary> 销毁时调用 </summary>
        protected virtual void OnDestroy() { }
    }
}
