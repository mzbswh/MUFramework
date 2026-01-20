namespace MUFramework
{
    /// <summary>
    /// UI生命周期接口
    /// </summary>
    public interface IUILifecycle
    {
        /// <summary>
        /// 创建时调用
        /// </summary>
        void OnCreate();

        /// <summary>
        /// 显示时调用
        /// </summary>
        void OnShow();

        /// <summary>
        /// 恢复时调用
        /// </summary>
        void OnResume();

        /// <summary>
        /// 暂停时调用
        /// </summary>
        void OnPause();

        /// <summary>
        /// 隐藏时调用
        /// </summary>
        void OnHide();

        /// <summary>
        /// 销毁时调用
        /// </summary>
        void OnDestroy();
    }
}
