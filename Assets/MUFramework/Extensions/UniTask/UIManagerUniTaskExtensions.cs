#if UNITASK_SUPPORT
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MUFramework
{
    public static class UIManagerUniTaskExtensions
    {
        public static UniTask<TWindow> OpenAsync<TWindow>(
            this UIManager manager,
            CancellationToken ct = default)
            where TWindow : UIWindow
        {
            var tcs = new UniTaskCompletionSource<TWindow>();
            ct.Register(() => tcs.TrySetCanceled());
            manager.OpenAsync<TWindow>(window => tcs.TrySetResult(window));
            return tcs.Task;
        }

        public static UniTask<TWindow> OpenAsync<TWindow, TData>(
            this UIManager manager,
            TData data,
            CancellationToken ct = default)
            where TWindow : UIWindow<TData>
        {
            var tcs = new UniTaskCompletionSource<TWindow>();
            ct.Register(() => tcs.TrySetCanceled());
            manager.OpenAsync<TWindow, TData>(data, window => tcs.TrySetResult(window));
            return tcs.Task;
        }

        public static UniTask<TWindow> OpenAsync<TWindow, T1>(
            this UIManager manager,
            T1 arg1,
            CancellationToken ct = default)
            where TWindow : UIWindow, IOpenArgs<T1>
        {
            var tcs = new UniTaskCompletionSource<TWindow>();
            ct.Register(() => tcs.TrySetCanceled());
            manager.OpenAsync<TWindow, T1>(arg1, window => tcs.TrySetResult(window));
            return tcs.Task;
        }

        public static UniTask CloseAsync(
            this UIManager manager,
            long uniqueId,
            CancellationToken ct = default)
        {
            var tcs = new UniTaskCompletionSource();
            ct.Register(() => tcs.TrySetCanceled());
            manager.Close(uniqueId, withAnimation: true, onComplete: () => tcs.TrySetResult());
            return tcs.Task;
        }
    }
}
#endif
