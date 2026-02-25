using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UIWidget基类 (通用UI组件, 默认附加在UIWindow上)
    /// </summary>
    public abstract class UIWidget
    {
        public bool Inited { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }

        public object Data { get; private set; }
        public UIWindow AttachedWindow { get; private set; }

        public bool UpdateByMono { get; private set; }

        /// <summary>
        /// 初始化UIWidget
        /// </summary>
        /// <param name="root">根GameObject</param>
        /// <param name="updateByMono">是否使用MonoAdapter管理Update和UpdatePerSecond</param>
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
        }

        // ===== 子类重写生命周期回调 =====

        /// <summary> 创建时调用 </summary>
        protected virtual void OnCreate() { }
        /// <summary> 设置Active时调用 </summary>
        protected virtual void OnSetActive(bool active) { }
        /// <summary> 更新时调用 </summary>
        protected virtual void OnUpdate(float deltaTime) { }
        /// <summary> 每秒更新时调用(通用逻辑) </summary>
        protected virtual void OnUpdatePerSecond() { }
        /// <summary> 销毁时调用 </summary>
        protected virtual void OnDestroy() { }

        /// <summary> 刷新UI时调用 </summary>
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
