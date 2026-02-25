using UnityEngine;

namespace MUFramework
{
    public class UIWidgetMonoAdapter : MonoBehaviour
    {
        public UIWidget Widget { get; private set; }

        private float _oneSecondTimer = 0f;

        public void Init(UIWidget widget)
        {
            Widget = widget;
        }

        private void Update()
        {
            Widget.Update(Time.deltaTime);
            _oneSecondTimer -= Time.deltaTime;
            if (_oneSecondTimer <= 0f)
            {
                _oneSecondTimer = 1f;
                Widget.UpdatePerSecond();
            }
        }

        private void OnDestroy()
        {
            Widget.Destroy();
        }
    }
}