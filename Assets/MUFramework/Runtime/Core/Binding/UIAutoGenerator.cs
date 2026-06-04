using System.Collections.Generic;
using UnityEngine;

namespace MUFramework
{
    public enum UIBindingTargetType
    {
        Window,
        Panel,
        Widget,
    }

    [DisallowMultipleComponent]
    public class UIAutoGenerator : MonoBehaviour
    {
        public UIBindingTargetType TargetBaseType = UIBindingTargetType.Window;
        public string TargetClassName;
        public string TargetNamespace;

        [Tooltip("主类脚本输出目录，留空使用 MUI 配置中的默认路径")]
        public string ScriptOutputDir;

        public List<UIBindingEntry> Entries = new();
    }
}
