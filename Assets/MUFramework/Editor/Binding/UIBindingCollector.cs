using System.Collections.Generic;
using UnityEngine;

namespace MUFramework.Editor
{
    [DisallowMultipleComponent]
    public class UIBindingCollector : MonoBehaviour
    {
        public string TargetClassName;
        public string TargetNamespace;
        public List<UIBindingEntry> Entries = new();
    }
}
