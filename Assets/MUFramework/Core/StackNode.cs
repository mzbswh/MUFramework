using System.Collections.Generic;

namespace MUFramework
{
    /// <summary>
    /// 栈节点数据结构
    /// </summary>
    public class StackNode
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// 窗口实例
        /// </summary>
        public UIWindow Window { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public UIState State { get; set; }

        /// <summary>
        /// 是否保持下层可见
        /// 一般全屏界面下面的界面都不可见，不可见一定暂停Update
        /// true: 下层显示
        /// </summary>
        public bool KeepBelowVisible { get; set; }

        /// <summary>
        /// 是否暂停下层Update
        /// 一般全屏界面暂停下层Update
        /// </summary>
        public bool PauseBelowUpdate { get; set; }

        /// <summary>
        /// 界面类型
        /// 用于判断某一个界面是否在最上层，如果上面还有一个跑马灯消息界面，
        /// 但是这个界面可以不参与最上层判断，即判断最上层界面时跳过这个界面
        /// </summary>
        public WindowType Type { get; set; }

        /// <summary>
        /// 被覆盖时的默认行为，默认为KeepRunning
        /// 如PauseAndHide、PauseButVisible
        /// </summary>
        public CoverBehavior WhenCovered { get; set; } = CoverBehavior.KeepRunning;

        /// <summary>
        /// 是否允许调用方覆盖覆盖行为
        /// </summary>
        public bool AllowOverrideCoverBehavior { get; set; } = true;

        /// <summary>
        /// 父界面ID（用于界面绑定）
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// 子界面ID列表
        /// </summary>
        public List<string> ChildrenIds { get; set; } = new List<string>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public StackNode()
        {
            State = UIState.Hidden;
            Type = WindowType.Normal;
            WhenCovered = CoverBehavior.KeepRunning;
            AllowOverrideCoverBehavior = true;
            KeepBelowVisible = false;
            PauseBelowUpdate = false;
        }
    }
}
