namespace MUFramework
{
    public enum CacheType
    {
        /// <summary> 不缓存 </summary>
        None,

        /// <summary> 过期时间 </summary>
        ExpireTime,

        /// <summary> 持久缓存 </summary>
        Persistent,
    }
}