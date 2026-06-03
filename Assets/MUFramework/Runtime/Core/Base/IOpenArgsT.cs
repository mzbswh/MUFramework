namespace MUFramework
{
    /// <summary>
    /// Window open argument interface for one argument.
    /// </summary>
    public interface IOpenArgs<T1>
    {
        void OnOpen(T1 arg1);
    }

    /// <summary> Window open argument interface for two arguments. </summary>
    public interface IOpenArgs<T1, T2>
    {
        void OnOpen(T1 arg1, T2 arg2);
    }

    /// <summary> Window open argument interface for three arguments. </summary>
    public interface IOpenArgs<T1, T2, T3>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3);
    }

    /// <summary> Window open argument interface for four arguments. </summary>
    public interface IOpenArgs<T1, T2, T3, T4>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    /// <summary> Window open argument interface for five arguments. </summary>
    public interface IOpenArgs<T1, T2, T3, T4, T5>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    [System.Obsolete("Use IOpenArgs<T> instead of IWindowOpenArgs<T>.")]
    public interface IWindowOpenArgs<T> : IOpenArgs<T> { }

    [System.Obsolete("Use IOpenArgs<T1,T2> instead of IWindowOpenArgs<T1,T2>.")]
    public interface IWindowOpenArgs<T1, T2> : IOpenArgs<T1, T2> { }

    [System.Obsolete("Use IOpenArgs<T1,T2,T3> instead of IWindowOpenArgs<T1,T2,T3>.")]
    public interface IWindowOpenArgs<T1, T2, T3> : IOpenArgs<T1, T2, T3> { }

    [System.Obsolete("Use IOpenArgs<T1,T2,T3,T4> instead of IWindowOpenArgs<T1,T2,T3,T4>.")]
    public interface IWindowOpenArgs<T1, T2, T3, T4> : IOpenArgs<T1, T2, T3, T4> { }

    [System.Obsolete("Use IOpenArgs<T1,T2,T3,T4,T5> instead of IWindowOpenArgs<T1,T2,T3,T4,T5>.")]
    public interface IWindowOpenArgs<T1, T2, T3, T4, T5> : IOpenArgs<T1, T2, T3, T4, T5> { }
}
