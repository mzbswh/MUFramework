using UnityEngine;

namespace MUFramework
{
    public abstract class UIWidget
    {
        public bool Inited { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public object Data { get; private set; }
        public UIWindow AttachedWindow { get; private set; }
        public bool UpdateByMono { get; private set; }

        public void Init(GameObject root, bool updateByMono = false)
        {
            Inited = true;
            GameObject = root;
            Transform = root.transform;
            UpdateByMono = updateByMono;
            if (UpdateByMono)
            {
                GameObject.GetOrAddComponent<UIWidgetMonoAdapter>().Init(this);
            }
            OnCreate();
            RefreshUI();
        }

        public void AttachTo(UIWindow window)
        {
            if (AttachedWindow == window) return;
            AttachedWindow = window;
            AttachedWindow.AttachWidget(this);
        }

        public void SetData(object data)
        {
            Data = data;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (!Inited) return;
            OnRefreshUI();
        }

        public void SetActive(bool active)
        {
            if (!Inited) return;
            GameObject.SetActive(active);
            OnSetActive(active);
        }

        public void Update(float deltaTime)
        {
            if (!Inited) return;
            OnUpdate(deltaTime);
        }

        public void UpdatePerSecond()
        {
            if (!Inited) return;
            OnUpdatePerSecond();
        }

        public void Destroy()
        {
            if (!Inited) return;
            OnDestroy();
            Inited = false;
        }

        internal void NotifyOpen() { if (Inited) OnOpen(); }
        internal void NotifyClose() { if (Inited) OnClose(); }
        internal void NotifyShow() { if (Inited) OnShow(); }
        internal void NotifyHide() { if (Inited) OnHide(); }
        internal void NotifyPause() { if (Inited) OnPause(); }
        internal void NotifyResume() { if (Inited) OnResume(); }

        protected virtual void OnCreate() { }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnSetActive(bool active) { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnUpdatePerSecond() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnRefreshUI() { }
    }

    public abstract class UIWidget<TData> : UIWidget
    {
        public new TData Data { get; private set; }

        public void SetData(TData data)
        {
            base.SetData(data);
            Data = data;
        }
    }

    public abstract class UIWidget<TData, TAttachedWindow> : UIWidget where TAttachedWindow : UIWindow
    {
        public new TData Data { get; private set; }
        public new TAttachedWindow AttachedWindow { get; private set; }

        public void SetData(TData data)
        {
            base.SetData(data);
            Data = data;
        }

        public void AttachTo(TAttachedWindow attachedWindow)
        {
            base.AttachTo(attachedWindow);
            AttachedWindow = attachedWindow;
        }
    }
}
