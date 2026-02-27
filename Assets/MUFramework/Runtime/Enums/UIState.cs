using System;

namespace MUFramework
{
    /// <summary>
    /// UI界面状态枚举
    /// </summary>
    [Flags]
    public enum UIState
    {
        None = 0,

        /// <summary>
        /// 可见但暂停（可见但不可更新）
        /// </summary>
        Paused = 1 << 0,

        /// <summary>
        /// 隐藏状态（不可见）
        /// </summary>
        Hidden = 1 << 1,

        /// <summary>
        /// Coverd
        /// </summary>
        Covered = 1 << 2,

        /// <summary>
        /// 正在关闭
        /// </summary>
        Closing = 1 << 3,

        /// <summary>
        /// 已关闭
        /// </summary>
        Closed = 1 << 4,

        /// <summary>
        /// 加载中
        /// </summary>
        Loading = 1 << 5,

        /// <summary>
        /// 加载完成
        /// </summary>
        Loaded = 1 << 6,
    }
}
