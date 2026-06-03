using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// Window-internal pure C# sub-panel. It is not managed by the global UI stack.
    /// </summary>
    public abstract class UIPanel
    {
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public UIWindow OwnerWindow { get; private set; }
        public bool IsActive { get; private set; }

        public void Init(GameObject root, UIWindow owner)
        {
            GameObject = root;
            Transform = root.transform;
            OwnerWindow = owner;
            AutoBindComponents();
            BindComponents();
            OnCreate();
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            GameObject.SetActive(true);
            OnActivate();
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            OnDeactivate();
            GameObject.SetActive(false);
        }

        public void Refresh() => OnRefresh();

        public void Destroy()
        {
            OnDestroy();
            IsActive = false;
            GameObject = null;
            Transform = null;
            OwnerWindow = null;
        }

        internal virtual void AutoBindComponents() { }

        protected virtual void BindComponents() { }
        protected virtual void OnCreate() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnRefresh() { }
        protected virtual void OnDestroy() { }
    }

    public abstract class UIPanel<TData> : UIPanel
    {
        protected TData Data { get; private set; }

        public void Activate(TData data)
        {
            Data = data;
            Activate();
        }

        protected sealed override void OnActivate() => OnActivate(Data);

        protected abstract void OnActivate(TData data);
    }
}
