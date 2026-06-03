using System;
using System.Collections.Generic;

namespace MUFramework.Editor
{
    public abstract class UIBindingNamingRule
    {
        public abstract IReadOnlyDictionary<string, Type> PrefixMap { get; }

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
    }
}
