using System;
using System.Collections.Generic;
using System.Text;

namespace MUFramework.Editor
{
    public abstract class UIBindingNamingRule
    {
        public abstract IReadOnlyDictionary<string, Type> PrefixMap { get; }

        public abstract string ToTargetClassName(UIBindingNamingContext context);

        public abstract string ToTargetNamespace(UIBindingNamingContext context);

        public virtual string ToFieldName(string nodeName, string matchedPrefix)
        {
            var trimmedPrefix = matchedPrefix.TrimEnd('_');
            var suffix = nodeName.Substring(matchedPrefix.Length);
            if (string.IsNullOrEmpty(suffix))
            {
                return "_" + trimmedPrefix.ToLower();
            }
            return "_" + char.ToLower(trimmedPrefix[0]) + trimmedPrefix.Substring(1) + suffix;
        }

        public (string fieldName, Type componentType)? TryMatch(string nodeName)
        {
            foreach (var (prefix, type) in PrefixMap)
            {
                if (nodeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return (ToFieldName(nodeName, prefix), type);
                }
            }
            return null;
        }

        protected static string ToPascalIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UIBindingTarget";
            }

            var builder = new StringBuilder();
            var upperNext = true;
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
                    upperNext = false;
                }
                else
                {
                    upperNext = true;
                }
            }

            if (builder.Length == 0)
            {
                return "UIBindingTarget";
            }

            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, "UI");
            }

            return builder.ToString();
        }
    }
}
