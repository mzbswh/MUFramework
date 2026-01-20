namespace MUFramework
{
    /// <summary>
    /// 界面被覆盖时的行为枚举
    /// </summary>
    public enum CoverBehavior
    {
        /// <summary>
        /// 保持运行（默认行为）
        /// </summary>
        KeepRunning,

        /// <summary>
        /// 暂停并隐藏
        /// </summary>
        PauseAndHide,

        /// <summary>
        /// 暂停但保持可见
        /// </summary>
        PauseButVisible
    }
}
