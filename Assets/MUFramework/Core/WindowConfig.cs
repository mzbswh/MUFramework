using System.Collections.Generic;

namespace MUFramework
{
    /// <summary>
    /// 界面配置数据
    /// </summary>
    [System.Serializable]
    public class WindowConfig
    {
        /// <summary>
        /// 界面ID
        /// </summary>
        public string WindowId;

        /// <summary>
        /// 是否允许多实例
        /// </summary>
        public bool AllowMultiInstance = false;

        /// <summary>
        /// 最大实例数量
        /// </summary>
        public int MaxInstances = 1;

        /// <summary>
        /// 溢出策略（当超过MaxInstances时）
        /// </summary>
        public OverflowPolicy OverflowPolicy = OverflowPolicy.Reject;

        /// <summary>
        /// 依赖的界面ID列表
        /// </summary>
        public List<string> Dependencies = new List<string>();

        /// <summary>
        /// 默认层级
        /// </summary>
        public UILayer DefaultLayer = UILayer.Default;

        /// <summary>
        /// 界面类型
        /// </summary>
        public WindowType WindowType = WindowType.Normal;

        /// <summary>
        /// 默认覆盖行为
        /// </summary>
        public CoverBehavior DefaultCoverBehavior = CoverBehavior.KeepRunning;

        /// <summary>
        /// 是否允许覆盖行为被覆盖
        /// </summary>
        public bool AllowOverrideCoverBehavior = true;
    }
}
