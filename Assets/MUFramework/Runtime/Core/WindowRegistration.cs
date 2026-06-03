using System;

namespace MUFramework
{
    public sealed class WindowRegistration
    {
        public string WindowId { get; }
        public Type WindowType { get; }
        public WindowOpenConfig DefaultConfig { get; }

        public WindowRegistration(string windowId, Type windowType, WindowOpenConfig defaultConfig)
        {
            WindowId = windowId;
            WindowType = windowType;
            DefaultConfig = defaultConfig;
        }
    }
}
