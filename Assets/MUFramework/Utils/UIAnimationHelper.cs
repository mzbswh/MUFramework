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
        IEnumerator PlayOpenAnimation(GameObject target);

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        IEnumerator PlayCloseAnimation(GameObject target);
    }

    /// <summary>
    /// 默认空动画实现
    /// </summary>
    public class DefaultUIAnimation : IUIAnimation
    {
        public IEnumerator PlayOpenAnimation(GameObject target)
        {
            yield return null;
        }

        public IEnumerator PlayCloseAnimation(GameObject target)
        {
            yield return null;
        }
    }
}
