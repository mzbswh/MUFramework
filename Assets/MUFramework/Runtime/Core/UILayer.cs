using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 背景层级
        /// </summary>
        Background = 0,

        /// <summary>
        /// 默认层级: 全屏UI
        /// </summary>
        Default,

        /// <summary>
        /// 弹窗层级
        /// </summary>
        Popup,

        /// <summary>
        /// 顶层层级：跑马灯消息UI
        /// </summary>
        Top,

        /// <summary>
        /// 系统层级：重要系统提示，如网络断开、需要重启等
        /// </summary>
        System,

        /// <summary>
        /// 调试层级：用于调试UI
        /// </summary>
        Debug,
    }
}
