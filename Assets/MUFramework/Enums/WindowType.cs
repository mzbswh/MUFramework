namespace MUFramework
{
    /// <summary>
    /// 界面类型枚举
    /// 用于判断界面是否参与最上层判断
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// 普通界面，参与最上层判断
        /// </summary>
        Normal,

        /// <summary>
        /// 特殊界面，不参与最上层判断（如跑马灯消息界面）
        /// </summary>
        Overlay
    }
}
