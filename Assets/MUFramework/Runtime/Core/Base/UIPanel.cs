namespace MUFramework
{
    /// <summary>
    /// UIPanel基类
    /// 子面板，如一个window有多个tab，每个tab显示不同panel
    /// panel与panel之间可嵌套
    /// </summary>
    public abstract class UIPanel : UIWindow
    {
        // Panel继承自Window，但通常不参与栈管理
        // 具体的栈管理逻辑由UIManager决定
    }
}
