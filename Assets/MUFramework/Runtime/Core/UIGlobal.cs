using System;

namespace MUFramework
{
    /// <summary>
    /// 全局事件工具
    /// </summary>
    public static class UIGlobal
    {
        public const string UI_EVENT_WINDOW_OPEN = "UIEvent_WindowOpen";
        public const string UI_EVENT_WINDOW_CLOSE = "UIEvent_WindowClose";
        public const string UI_EVENT_WINDOW_SHOW = "UIEvent_WindowShow";
        public const string UI_EVENT_WINDOW_HIDE = "UIEvent_WindowHide";
        public const string UI_EVENT_WINDOW_RESUME = "UIEvent_WindowResume";
        public const string UI_EVENT_WINDOW_PAUSE = "UIEvent_WindowPause";
        public const string UI_EVENT_WINDOW_DESTROY = "UIEvent_WindowDestroy";

        public static Action<LogLevel, string> LogHandler;
        public static Action<string> UIEventHandler;
        public static Func<string, (bool, WindowOpenConfig)> GetWindowOpenConfigFunc;

        /// <summary>
        /// 每个UILayer的Sorting Order间隔
        /// </summary>
        public static int LayerSortingOrderInterval = 1_000;

        /// <summary>
        /// Layer里的界面Sorting Order间隔（每个layer最大支持界面数量=（LayerSortingOrderInterval / InLayerSortingOrderInterval））
        /// </summary>
        public static int InLayerSortingOrderInterval = 20;

        public static IUIAnimation DefaultAnimationHelper = new DefaultUIAnimation();

        public static void ApplyConfig(GlobalConfigData config)
        {
            LayerSortingOrderInterval = config.LayerSortingOrderInterval;
            InLayerSortingOrderInterval = config.InLayerSortingOrderInterval;
        }
    }
}