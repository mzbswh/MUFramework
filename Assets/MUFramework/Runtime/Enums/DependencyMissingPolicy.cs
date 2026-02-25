namespace MUFramework
{
    /// <summary>
    /// 依赖缺失策略
    /// </summary>
    public enum DependencyMissingPolicy
    {
        /// <summary> 不管依赖的界面是否打开，都打开当前界面 </summary>
        OpenAnyway,

        /// <summary> 如果依赖的界面没有打开，则不打开当前界面 </summary>
        NotOpen,

        /// <summary> 仅打开缺失的依赖界面 </summary>
        OpenMissingDependency,

        /// <summary> 重新打开所有依赖的界面 </summary>
        ReOpenAllDependencies,
    }
}
