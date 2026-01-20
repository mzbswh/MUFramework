using UnityEngine;
using System.Collections;

namespace MUFramework
{
    /// <summary>
    /// UIWindow基类
    /// 一个window代表一个完整的UI界面，如商店
    /// </summary>
    public abstract class UIWindow : MonoBehaviour, IUILifecycle
    {
        /// <summary>
        /// 窗口唯一ID
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// 窗口ID（用于配置查找）
        /// </summary>
        public string WindowId { get; set; }

        /// <summary>
        /// 所属层级
        /// </summary>
        public UILayer Layer { get; set; }

        /// <summary>
        /// Canvas组件
        /// </summary>
        protected Canvas Canvas { get; private set; }

        /// <summary>
        /// 是否已创建
        /// </summary>
        public bool IsCreated { get; private set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 动画辅助器
        /// </summary>
        protected IUIAnimation AnimationHelper { get; set; }

        /// <summary>
        /// 是否使用动画
        /// </summary>
        public bool UseAnimation { get; set; } = false;

        protected virtual void Awake()
        {
            // 自动创建Canvas
            CreateCanvas();
        }

        protected virtual void Start()
        {
            // SafeArea适配
            SafeAreaAdapter.AdaptSafeArea(gameObject);
        }

        /// <summary>
        /// 创建Canvas
        /// </summary>
        private void CreateCanvas()
        {
            Canvas = GetComponent<Canvas>();
            if (Canvas == null)
            {
                Canvas = gameObject.AddComponent<Canvas>();
            }

            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = (int)Layer;

            // 添加CanvasScaler
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 添加GraphicRaycaster
            var raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        /// <summary>
        /// 设置Canvas的Sorting Order
        /// </summary>
        public void SetSortingOrder(int order)
        {
            if (Canvas != null)
            {
                Canvas.sortingOrder = order;
            }
        }

        /// <summary>
        /// 创建时调用
        /// </summary>
        public virtual void OnCreate()
        {
            IsCreated = true;
        }

        /// <summary>
        /// 显示时调用
        /// </summary>
        public virtual void OnShow()
        {
            IsVisible = true;
            gameObject.SetActive(true);

            if (UseAnimation && AnimationHelper != null)
            {
                StartCoroutine(PlayOpenAnimationCoroutine());
            }
        }

        /// <summary>
        /// 恢复时调用
        /// </summary>
        public virtual void OnResume()
        {
            IsActive = true;
        }

        /// <summary>
        /// 暂停时调用
        /// </summary>
        public virtual void OnPause()
        {
            IsActive = false;
        }

        /// <summary>
        /// 隐藏时调用
        /// </summary>
        public virtual void OnHide()
        {
            IsVisible = false;

            if (UseAnimation && AnimationHelper != null)
            {
                StartCoroutine(PlayCloseAnimationCoroutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 执行销毁逻辑
        /// </summary>
        private void DoDestroy()
        {
            IsCreated = false;
            IsVisible = false;
            IsActive = false;
        }

        /// <summary>
        /// 销毁时调用（接口实现）
        /// </summary>
        void IUILifecycle.OnDestroy()
        {
            DoDestroy();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void Close()
        {
            UIManager.Instance?.CloseWindow(UniqueId);
        }

        /// <summary>
        /// 播放打开动画协程
        /// </summary>
        private IEnumerator PlayOpenAnimationCoroutine()
        {
            if (AnimationHelper != null)
            {
                yield return StartCoroutine(AnimationHelper.PlayOpenAnimation(gameObject));
            }
        }

        /// <summary>
        /// 播放关闭动画协程
        /// </summary>
        private IEnumerator PlayCloseAnimationCoroutine()
        {
            if (AnimationHelper != null)
            {
                yield return StartCoroutine(AnimationHelper.PlayCloseAnimation(gameObject));
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Unity的OnDestroy回调
        /// </summary>
        protected virtual void OnDestroy()
        {
            DoDestroy();
        }
    }
}
