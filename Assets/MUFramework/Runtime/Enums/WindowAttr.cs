using System;

namespace MUFramework
{
    /// <summary> 界面属性枚举 </summary>
    [Flags]
    public enum WindowAttr
    {
        /// <summary> 无属性 </summary>
        None = 0,

        /// <summary> 不参与是否覆盖下层检查（如跑马灯消息界面） </summary>
        SkipCoveredCheck = 1 << 0,
    }
}
