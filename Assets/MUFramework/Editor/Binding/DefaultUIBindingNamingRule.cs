using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MUFramework.Editor
{
    public class DefaultUIBindingNamingRule : UIBindingNamingRule
    {
        private static readonly Dictionary<string, Type> _prefixMap = new()
        {
            { "Btn_", typeof(Button) },
            { "Txt_", typeof(Text) },
            { "Img_", typeof(Image) },
            { "Raw_", typeof(RawImage) },
            { "Sld_", typeof(Slider) },
            { "Tog_", typeof(Toggle) },
            { "Inp_", typeof(InputField) },
            { "Scr_", typeof(ScrollRect) },
            { "Rect_", typeof(RectTransform) },
            { "Go_", typeof(GameObject) },
        };

        public override IReadOnlyDictionary<string, Type> PrefixMap => _prefixMap;

        public override string ToTargetClassName(UIGeneratorContext context)
        {
            return ToPascalIdentifier(context != null ? context.RootGameObjectName : string.Empty);
        }

        public override string ToTargetNamespace(UIGeneratorContext context)
        {
            return string.Empty;
        }
    }
}
