namespace MUFramework
{
    /// <summary>
    /// UI界面状态枚举
    /// </summary>
    public enum UIState
    {
        /// <summary>
        /// 激活状态（可见且可更新）
        /// </summary>
        Active,

        /// <summary>
        /// 可见但暂停（可见但不可更新）
        /// </summary>
        VisibleButPaused,

        /// <summary>
        /// 可见且激活（可见且可更新）
        /// </summary>
        VisibleAndActive,

        /// <summary>
        /// 隐藏状态（不可见）
        /// </summary>
        Hidden
    }
}
