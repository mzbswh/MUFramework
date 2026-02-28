using System.Collections.Generic;

namespace MUFramework
{
    /// <summary>
    /// 界面配置数据
    /// </summary>
    public struct WindowOpenConfig
    {
        /// <summary> 界面ID(唯一标识一个界面对象，一个ID可以对应多个实例) </summary>
        public string WindowId;

        /// <summary> 是否允许多实例(为false时默认最大实例为，此时OverflowPolicy控制是重新打开还是拒绝打开) </summary>
        public bool AllowMultiInstance;

        /// <summary> 最大实例数量(如果<=0，则不限制实例数量) </summary>
        public int MaxInstances;

        /// <summary> 溢出策略（当超过MaxInstances时） </summary>
        public OverflowPolicy OverflowPolicy;

        /// <summary> 依赖的界面ID列表（当依赖的界面打开时，当前界面才能打开） </summary>
        public List<string> Dependencies;

        /// <summary> 依赖缺失策略（当依赖的界面没有打开时） </summary>
        public DependencyMissingPolicy DependencyMissingPolicy;

        /// <summary> 层级 </summary>
        public UILayer Layer;

        /// <summary> 界面属性 </summary>
        public WindowAttr WindowAttr;

        /// <summary> 覆盖行为(当其他界面的开启行为不影响当前界面时生效) </summary>
        public CoverdBehavior WhenCovered;

        /// <summary> 界面打开行为（如全屏界面打开时，隐藏下面界面） </summary>
        public OpenBehavior OpenBehavior;

        /// <summary> 缓存类型 </summary>
        public CacheType CacheType;

        /// <summary> 过期时间(当CacheType为ExpireTime时生效，单位：秒) </summary>
        public float ExpireTime;

        /// <summary> UI动画 </summary>
        public IUIAnimation UIAnimation;

        public static WindowOpenConfig Default => new WindowOpenConfig
        {
            AllowMultiInstance = false,
            MaxInstances = 1,
            OverflowPolicy = OverflowPolicy.CloseOldest,
            Dependencies = new List<string>(),
            DependencyMissingPolicy = DependencyMissingPolicy.OpenAnyway,
            Layer = UILayer.Default,
            WindowAttr = WindowAttr.None,
            WhenCovered = CoverdBehavior.Normal,
            OpenBehavior = OpenBehavior.KeepBelow,
            CacheType = CacheType.None,
            ExpireTime = 0f,
            UIAnimation = null,
        };
    }
}
