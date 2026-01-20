using System.Collections.Generic;

namespace MUFramework
{
    /// <summary>
    /// 窗口配置数据容器（用于JSON序列化）
    /// </summary>
    [System.Serializable]
    public class WindowConfigData
    {
        /// <summary>
        /// 所有窗口配置列表
        /// </summary>
        public List<WindowConfig> Windows = new List<WindowConfig>();
    }
}
