using System;
using UnityEngine;

namespace MUFramework.Editor
{
    [Serializable]
    public class UIBindingEntry
    {
        public string FieldName;
        public string ComponentType;
        public Component ComponentRef;
        public string Path;
    }
}
