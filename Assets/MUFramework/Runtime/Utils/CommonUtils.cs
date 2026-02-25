using UnityEngine;

namespace MUFramework
{
    public static class CommonUtils
    {
        public static bool HasState(this UIState state, UIState flag) => (state & flag) != 0;

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent(out T component))
            {
                return gameObject.AddComponent<T>();
            }
            return component;
        }

        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }
    }
}