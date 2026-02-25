using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// 全局配置数据
    /// </summary>
    [System.Serializable]
    public class GlobalConfigData : ScriptableObject
    {
        /// <summary>
        /// 每个UILayer的Sorting Order间隔
        /// </summary>
        public int LayerSortingOrderInterval;

        /// <summary>
        /// Layer里的界面Sorting Order间隔（每个layer最大支持界面数量=（LayerSortingOrderInterval / InLayerSortingOrderInterval））
        /// </summary>
        public int InLayerSortingOrderInterval;
    }
}