using System.Collections;
using UnityEngine;

namespace MUFramework
{
    /// <summary>
    /// UI资源加载接口
    /// 由调用方实现具体加载逻辑
    /// </summary>
    public interface IUIResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        GameObject LoadGameObject(string windowId);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        IEnumerator LoadGameObjectAsync(string windowId, System.Action<GameObject> callback);
    }
}
