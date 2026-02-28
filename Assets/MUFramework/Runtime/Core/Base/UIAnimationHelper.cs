using System;
using System.Collections;
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UI动画辅助接口
    /// </summary>
    public interface IUIAnimation
    {
        /// <summary>
        /// 播放打开动画
        /// </summary>
        long PlayOpen(GameObject target, Action completeCallback);

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        long PlayClose(GameObject target, Action completeCallback);

        /// <summary>
        /// 停止播放动画
        /// </summary>
        void Stop(long id);
    }

    /// <summary>
    /// 默认空动画实现
    /// </summary>
    public class DefaultUIAnimation : IUIAnimation
    {
        public long PlayOpen(GameObject target, Action completeCallback)
        {
            completeCallback?.Invoke();
            return 0;
        }

        public long PlayClose(GameObject target, Action completeCallback)
        {
            completeCallback?.Invoke();
            return 0;
        }

        public void Stop(long id)
        {
            
        }
    }
}
