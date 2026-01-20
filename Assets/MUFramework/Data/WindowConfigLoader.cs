using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace MUFramework
{
    /// <summary>
    /// 窗口配置加载器
    /// 从JSON文件加载配置
    /// </summary>
    public static class WindowConfigLoader
    {
        private static Dictionary<string, WindowConfig> _configCache = new Dictionary<string, WindowConfig>();

        /// <summary>
        /// 加载所有窗口配置
        /// </summary>
        public static Dictionary<string, WindowConfig> LoadConfigs(string jsonPath)
        {
            if (_configCache.Count > 0)
                return _configCache;

            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Window config file not found: {fullPath}");
                return _configCache;
            }

            string jsonContent = File.ReadAllText(fullPath);
            WindowConfigData configData = JsonUtility.FromJson<WindowConfigData>(jsonContent);

            if (configData != null && configData.Windows != null)
            {
                foreach (var config in configData.Windows)
                {
                    _configCache[config.WindowId] = config;
                }
            }

            return _configCache;
        }

        /// <summary>
        /// 获取指定窗口配置
        /// </summary>
        public static WindowConfig GetConfig(string windowId)
        {
            _configCache.TryGetValue(windowId, out var config);
            return config;
        }

        /// <summary>
        /// 清除配置缓存
        /// </summary>
        public static void ClearCache()
        {
            _configCache.Clear();
        }
    }
}
