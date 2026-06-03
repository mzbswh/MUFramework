using System;

namespace MUFramework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UIWindowConfigAttribute : Attribute
    {
        public string WindowId { get; set; }
        public UILayer Layer { get; set; } = UILayer.Default;
        public CoveredBehavior WhenCovered { get; set; } = CoveredBehavior.Normal;
        public OpenBehavior OpenBehavior { get; set; } = OpenBehavior.KeepBelow;
        public CacheType CacheType { get; set; } = CacheType.None;
        public float ExpireTime { get; set; } = 60f;
        public bool AllowMultiInstance { get; set; } = false;
        public int MaxInstances { get; set; } = 1;
        public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.CloseOldest;
        public WindowAttr WindowAttr { get; set; } = WindowAttr.None;

        public UIWindowConfigAttribute() { }

        public UIWindowConfigAttribute(UILayer layer = UILayer.Default, CacheType cache = CacheType.None)
        {
            Layer = layer;
            CacheType = cache;
        }

        public WindowOpenConfig ToConfig(string resolvedWindowId) => new WindowOpenConfig
        {
            WindowId = resolvedWindowId,
            Layer = Layer,
            WhenCovered = WhenCovered,
            OpenBehavior = OpenBehavior,
            CacheType = CacheType,
            ExpireTime = ExpireTime,
            AllowMultiInstance = AllowMultiInstance,
            MaxInstances = MaxInstances,
            OverflowPolicy = OverflowPolicy,
            WindowAttr = WindowAttr,
        };
    }
}
