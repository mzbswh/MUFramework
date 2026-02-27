namespace MUFramework
{
    public interface IWindowOpenArgs<T>
    {    
        void OnOpen(T arg);
    }

    public interface IWindowOpenArgs<T1, T2>
    {
        void OnOpen(T1 arg1, T2 arg2);
    }

    public interface IWindowOpenArgs<T1, T2, T3>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3);
    }

    public interface IWindowOpenArgs<T1, T2, T3, T4>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }

    public interface IWindowOpenArgs<T1, T2, T3, T4, T5>
    {
        void OnOpen(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }
}