using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    [DisallowMultipleComponent]
    public class UIBindingCollector : MonoBehaviour
    {
        public string TargetClassName;
        public string TargetNamespace;
        public List<UIBindingEntry> Entries = new();

        [HideInInspector]
        public bool HasAutoScanned;
    }
}
