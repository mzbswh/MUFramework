using System;
using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UIWindow base class. One window represents a complete UI screen.
    /// </summary>
    public abstract class UIWindow
    {
        public long UniqueId => _uniqueId;
        public GameObject GameObject => _stackNode?.GameObject;
        public Transform Transform => _stackNode?.Transform;
        public CanvasGroup CanvasGroup => _stackNode?.CanvasGroup;
        public Canvas Canvas => _stackNode?.Canvas;
        public IUIAnimation UIAnimation => _stackNode?.UIAnimation;
        public bool IsPause => _stackNode != null && _stackNode.IsPause;
        public bool IsCovered => _stackNode != null && _stackNode.IsCovered;
        public bool IsHidden => _stackNode != null && _stackNode.IsHidden;
        public bool IsClosing => _stackNode != null && _stackNode.IsClosing;
        public bool IsClosed => _stackNode == null || _stackNode.IsClosed;

        private readonly List<UIWidget> _widgets = new();
        private UIStackNode _stackNode;
        private long _uniqueId;
        private bool _hasCreated;
        private long _animUniqueId;
        private int _animationVersion;

        public void Init(UIStackNode stackNode)
        {
            _stackNode = stackNode;
            _uniqueId = stackNode.UniqueId;
            AutoBindComponents();
            BindComponents();
            if (_hasCreated) return;
            _hasCreated = true;
            OnCreate();
        }

        internal virtual void OnOpenInternal(object[] args)
        {
            if (!TryInvokeTypedOpen(args))
            {
                OnOpen();
            }
            NotifyWidgetsOpen();
        }

        protected void NotifyWidgetsOpen()
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].NotifyOpen();
            }
        }

        internal void OnMessageInternal(string msg, object[] args) => OnMessage(msg, args);

        internal virtual void AutoBindComponents() { }

        protected virtual void BindComponents() { }

        internal void OnPauseInternal(bool pause)
        {
            if (pause)
            {
                OnPause();
                for (int i = 0; i < _widgets.Count; i++)
                {
                    _widgets[i].NotifyPause();
                }
            }
            else
            {
                OnResume();
                for (int i = 0; i < _widgets.Count; i++)
                {
                    _widgets[i].NotifyResume();
                }
            }
        }

        internal void OnCoverInternal(bool covered)
        {
            if (covered) OnCovered();
            else OnUncovered();
        }

        public void Show(bool withAnimation = true, Action onComplete = null)
        {
            var target = GameObject;
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            OnBeforeShow();
            SetInteractable(false);
            target.SetActive(true);
            var version = BeginAnimation();

            if (withAnimation && UIAnimation != null)
            {
                PlayAnimation(
                    play: complete => UIAnimation.PlayOpen(target, complete),
                    complete: () => CompleteShow(version, onComplete));
            }
            else
            {
                CompleteShow(version, onComplete);
            }
        }

        private void CompleteShow(int version, Action onComplete)
        {
            if (!IsCurrentAnimation(version)) return;
            _animUniqueId = 0;
            SetInteractable(true);
            GameObject.SetActive(true);
            OnShow();
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].NotifyShow();
            }
            onComplete?.Invoke();
        }

        public void Hide(bool withAnimation = true, Action onComplete = null)
        {
            var target = GameObject;
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            OnBeforeHide();
            SetInteractable(false);
            var version = BeginAnimation();

            if (withAnimation && UIAnimation != null)
            {
                PlayAnimation(
                    play: complete => UIAnimation.PlayClose(target, complete),
                    complete: () => CompleteHide(version, onComplete));
            }
            else
            {
                CompleteHide(version, onComplete);
            }
        }

        private void CompleteHide(int version, Action onComplete)
        {
            if (!IsCurrentAnimation(version)) return;
            _animUniqueId = 0;
            GameObject.SetActive(false);
            OnHide();
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].NotifyHide();
            }
            onComplete?.Invoke();
        }

        private int BeginAnimation()
        {
            UIAnimation?.Stop(_animUniqueId);
            _animUniqueId = 0;
            return ++_animationVersion;
        }

        private bool IsCurrentAnimation(int version)
            => _stackNode != null && _animationVersion == version;

        private void PlayAnimation(Func<Action, long> play, Action complete)
        {
            var completed = false;
            void CompleteOnce()
            {
                if (completed) return;
                completed = true;
                complete();
            }

            try
            {
                var id = play(CompleteOnce);
                _animUniqueId = completed ? 0 : id;
            }
            catch (Exception e)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error, $"[MUI] UI animation failed: {e}");
                CompleteOnce();
            }
        }

        public void Close(bool withAnimation = true, Action onComplete = null)
            => UIManager.Instance.Close(UniqueId, withAnimation, onComplete);

        internal void CompleteClose()
        {
            OnClose();
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].NotifyClose();
            }
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

        public void Destroy()
        {
            UIAnimation?.Stop(_animUniqueId);
            _animUniqueId = 0;
            _animationVersion++;
            OnDestroy();
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].Destroy();
            }
            _widgets.Clear();
            _stackNode = null;
        }

        public void SetInteractable(bool interactable)
        {
            if (CanvasGroup == null) return;
            CanvasGroup.interactable = interactable;
            CanvasGroup.blocksRaycasts = interactable;
        }

        public void AttachWidget(UIWidget widget)
        {
            if (_widgets.Contains(widget)) return;
            _widgets.Add(widget);
            widget.AttachTo(this);
        }

        protected virtual void OnCreate() { }
        protected virtual void OnOpen() { }
        protected virtual void OnBeforeShow() { }
        protected virtual void OnShow() { }
        protected virtual void OnResume() { }
        protected virtual void OnCovered() { }
        protected virtual void OnUncovered() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnUpdatePerSecond() { }
        protected virtual void OnPause() { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnHide() { }
        protected virtual void OnClose() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnMessage(string msg, params object[] args) { }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void OnOpenInternal_ForTest(object[] args) => OnOpenInternal(args);
        internal void OnPauseInternal_ForTest(bool pause) => OnPauseInternal(pause);
#endif

        private bool TryInvokeTypedOpen(object[] args)
        {
            if (args == null || args.Length == 0 || args.Length > 5) return false;
            var interfaces = GetType().GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var interfaceType = interfaces[i];
                if (!interfaceType.IsGenericType) continue;
                var definition = interfaceType.GetGenericTypeDefinition();
                if (definition != typeof(IOpenArgs<>) &&
                    definition != typeof(IOpenArgs<,>) &&
                    definition != typeof(IOpenArgs<,,>) &&
                    definition != typeof(IOpenArgs<,,,>) &&
                    definition != typeof(IOpenArgs<,,,,>))
                {
                    continue;
                }
                var genericArgs = interfaceType.GetGenericArguments();
                if (genericArgs.Length != args.Length) continue;
                if (!CanAcceptArgs(genericArgs, args)) continue;
                interfaceType.GetMethod(nameof(OnOpen))?.Invoke(this, args);
                return true;
            }
            return false;
        }

        private static bool CanAcceptArgs(Type[] parameterTypes, object[] args)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (args[i] == null)
                {
                    if (parameterTypes[i].IsValueType && Nullable.GetUnderlyingType(parameterTypes[i]) == null)
                    {
                        return false;
                    }
                    continue;
                }
                if (!parameterTypes[i].IsAssignableFrom(args[i].GetType()))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
