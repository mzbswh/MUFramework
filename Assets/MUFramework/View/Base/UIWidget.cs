using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UIWidget基类
    /// UI小组件，如CoinWidget，通常是一个可复用的对象
    /// 不参与栈管理
    /// </summary>
    public abstract class UIWidget : MonoBehaviour
    {
        /// <summary>
        /// Widget ID
        /// </summary>
        public string WidgetId { get; set; }

        /// <summary>
        /// 初始化Widget
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// 更新Widget数据
        /// </summary>
        public virtual void UpdateData(object data)
        {
        }

        /// <summary>
        /// 显示Widget
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏Widget
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
