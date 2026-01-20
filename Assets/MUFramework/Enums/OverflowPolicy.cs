namespace MUFramework
{
    /// <summary>
    /// 溢出策略枚举（当超过MaxInstances时的处理方式）
    /// </summary>
    public enum OverflowPolicy
    {
        /// <summary>
        /// 拒绝打开新实例
        /// </summary>
        Reject,

        /// <summary>
        /// 关闭最旧的实例
        /// </summary>
        CloseOldest,

        /// <summary>
        /// 关闭最新的实例
        /// </summary>
        CloseNewest
    }
}
