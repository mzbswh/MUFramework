using UnityEngine;

namespace MUFramework
{
    public partial class UIManager
    {
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            _updateSnapshot.Clear();
            foreach (var node in _allWindows.Values)
            {
                _updateSnapshot.Add(node);
            }

            for (int i = 0; i < _updateSnapshot.Count; i++)
            {
                var node = _updateSnapshot[i];
                if (node.IsLoaded && !node.IsPause && !node.IsClosing)
                {
                    node.Window.Update(dt);
                }
            }

            _perSecondTimer += dt;
            if (_perSecondTimer >= PER_SECOND_INTERVAL)
            {
                _perSecondTimer = 0f;
                for (int i = 0; i < _updateSnapshot.Count; i++)
                {
                    var node = _updateSnapshot[i];
                    if (node.IsLoaded && !node.IsPause && !node.IsClosing)
                    {
                        node.Window.UpdatePerSecond();
                    }
                }
            }

            _cacheCleanTimer += dt;
            if (_cacheCleanTimer >= CACHE_CLEAN_INTERVAL)
            {
                _cacheCleanTimer = 0f;
                CleanExpiredCache();
            }
        }
    }
}
