using System;
using System.Collections.Generic;
using System.Reflection;

namespace MUFramework
{
    public partial class UIManager
    {
        public void Register(WindowRegistration registration)
        {
            if (registration == null || string.IsNullOrEmpty(registration.WindowId)) return;
            _registry[registration.WindowId] = registration;
        }

        public void ScanAndRegisterAll()
        {
            var windowType = typeof(UIWindow);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.FullName;
                if (name.StartsWith("Unity") ||
                    name.StartsWith("System") ||
                    name.StartsWith("mscorlib") ||
                    name.StartsWith("Mono"))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (type.IsAbstract || !windowType.IsAssignableFrom(type)) continue;
                    var attr = type.GetCustomAttribute<UIWindowConfigAttribute>(inherit: false);
                    if (attr == null) continue;

                    var windowId = string.IsNullOrEmpty(attr.WindowId) ? type.Name : attr.WindowId;
                    Register(new WindowRegistration(windowId, type, attr.ToConfig(windowId)));
                }
            }
        }

        internal WindowRegistration GetRegistration(string windowId)
        {
            _registry.TryGetValue(windowId, out var registration);
            return registration;
        }

        internal WindowOpenConfig ResolveConfig(string windowId, Action<WindowOpenConfig> configOverride = null)
        {
            var registration = GetRegistration(windowId);
            if (registration == null)
            {
                UIGlobal.LogHandler?.Invoke(LogLevel.Error,
                    $"[MUI] Window '{windowId}' not registered. Call Register() or ScanAndRegisterAll() first.");
                return null;
            }

            var source = registration.DefaultConfig;
            var config = new WindowOpenConfig
            {
                WindowId = source.WindowId,
                Layer = source.Layer,
                WhenCovered = source.WhenCovered,
                OpenBehavior = source.OpenBehavior,
                CacheType = source.CacheType,
                ExpireTime = source.ExpireTime,
                AllowMultiInstance = source.AllowMultiInstance,
                MaxInstances = source.MaxInstances,
                OverflowPolicy = source.OverflowPolicy,
                WindowAttr = source.WindowAttr,
                UIAnimation = source.UIAnimation,
                Dependencies = source.Dependencies == null ? null : new List<string>(source.Dependencies),
                DependencyMissingPolicy = source.DependencyMissingPolicy,
            };
            configOverride?.Invoke(config);
            return config;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal WindowRegistration GetRegistration_ForTest(string windowId)
            => GetRegistration(windowId);
#endif
    }
}
