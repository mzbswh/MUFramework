using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// SafeArea适配器
    /// 自动查找名为"SafeAreaContent"的节点并进行适配
    /// </summary>
    public static class SafeAreaAdapter
    {
        /// <summary>
        /// 适配指定GameObject的SafeArea
        /// </summary>
        public static void AdaptSafeArea(GameObject target)
        {
            if (target == null)
                return;

            // 查找名为"SafeAreaContent"的节点
            Transform safeAreaContent = target.transform.Find("SafeAreaContent");
            if (safeAreaContent == null)
                return;

            RectTransform rectTransform = safeAreaContent.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            // 获取SafeArea
            Rect safeArea = Screen.safeArea;
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            // 计算屏幕比例
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 计算SafeArea在Canvas中的位置
            float anchorMinX = safeArea.x / screenWidth;
            float anchorMinY = safeArea.y / screenHeight;
            float anchorMaxX = (safeArea.x + safeArea.width) / screenWidth;
            float anchorMaxY = (safeArea.y + safeArea.height) / screenHeight;

            // 设置锚点
            rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
