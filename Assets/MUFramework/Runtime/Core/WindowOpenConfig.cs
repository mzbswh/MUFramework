using System.Collections.Generic;

namespace MUFramework
{
    public sealed class WindowOpenConfig
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
        public List<string> Dependencies { get; set; }
        public DependencyMissingPolicy DependencyMissingPolicy { get; set; } = DependencyMissingPolicy.OpenAnyway;
        public WindowAttr WindowAttr { get; set; } = WindowAttr.None;
        public IUIAnimation UIAnimation { get; set; }

        public static WindowOpenConfig Default => new WindowOpenConfig();
    }
}
