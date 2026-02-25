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
        void PlayOpen(GameObject target, Action completeCallback);

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        void PlayClose(GameObject target, Action completeCallback);
    }

    /// <summary>
    /// 默认空动画实现
    /// </summary>
    public class DefaultUIAnimation : IUIAnimation
    {
        public void PlayOpen(GameObject target, Action completeCallback)
        {
            completeCallback?.Invoke();
        }

        public void PlayClose(GameObject target, Action completeCallback)
        {
            completeCallback?.Invoke();
        }
    }
}
