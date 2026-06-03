namespace MUFramework
{
    /// <summary>
    /// Typed window base for a single structured data object.
    /// </summary>
    public abstract class UIWindow<TData> : UIWindow, IOpenArgs<TData>
    {
        public abstract void OnOpen(TData data);

        internal override void OnOpenInternal(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is TData data)
            {
                OnOpen(data);
            }
            else
            {
                OnOpen(default);
            }
            NotifyWidgetsOpen();
        }
    }
}
