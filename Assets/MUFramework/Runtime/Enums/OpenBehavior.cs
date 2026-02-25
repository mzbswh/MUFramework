namespace MUFramework
{
    /// <summary> 界面打开行为（影响下面界面的显示状态） </summary>
    public enum OpenBehavior
    {
        /// <summary> 保持当前状态(当前界面打开时，下面界面保持当前状态) </summary>
        KeepBelow,

        /// <summary> 暂停(当前界面打开时，下面界面暂停) </summary>
        PauseBleow,

        /// <summary> 隐藏(当前界面打开时，下面界面隐藏) </summary>
        HideBelow,
    }
}