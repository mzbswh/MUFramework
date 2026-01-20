using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 默认层级
        /// </summary>
        Default = 0,

        /// <summary>
        /// 弹窗层级
        /// </summary>
        Popup = 100,

        /// <summary>
        /// 顶层层级
        /// </summary>
        Top = 200,

        /// <summary>
        /// 系统层级
        /// </summary>
        System = 300
    }

    /// <summary>
    /// UI层级配置
    /// </summary>
    [System.Serializable]
    public class LayerConfig
    {
        /// <summary>
        /// 层级类型
        /// </summary>
        public UILayer layer;

        /// <summary>
        /// Canvas的Sorting Order
        /// </summary>
        public int sortingOrder;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool enabled = true;
    }
}
