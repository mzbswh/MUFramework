namespace MUFramework
{
    /// <summary>
    /// 覆盖配置
    /// 界面push时的配置选项
    /// </summary>
    public class OverlayConfig
    {
        /// <summary>
        /// 是否保持下层可见
        /// </summary>
        public bool KeepBelowVisible { get; set; }

        /// <summary>
        /// 是否暂停下层Update
        /// </summary>
        public bool PauseBelowUpdate { get; set; }

        /// <summary>
        /// 被覆盖时的行为
        /// </summary>
        public CoverBehavior WhenCovered { get; set; } = CoverBehavior.KeepRunning;

        /// <summary>
        /// 是否允许覆盖行为被覆盖
        /// </summary>
        public bool AllowOverrideCoverBehavior { get; set; } = true;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static OverlayConfig Default => new OverlayConfig
        {
            KeepBelowVisible = false,
            PauseBelowUpdate = false,
            WhenCovered = CoverBehavior.KeepRunning,
            AllowOverrideCoverBehavior = true
        };

        /// <summary>
        /// 全屏配置（隐藏下层）
        /// </summary>
        public static OverlayConfig FullScreen => new OverlayConfig
        {
            KeepBelowVisible = false,
            PauseBelowUpdate = true,
            WhenCovered = CoverBehavior.PauseAndHide,
            AllowOverrideCoverBehavior = true
        };

        /// <summary>
        /// 弹窗配置（保持下层可见但暂停）
        /// </summary>
        public static OverlayConfig Popup => new OverlayConfig
        {
            KeepBelowVisible = true,
            PauseBelowUpdate = true,
            WhenCovered = CoverBehavior.PauseButVisible,
            AllowOverrideCoverBehavior = true
        };
    }
}
