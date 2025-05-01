namespace Snail.Abstractions.Distribution.Attributes
{
    /// <summary>
    /// 特性标签：分布式缓存配置选项
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CacheAttribute : Attribute
    {
        /// <summary>
        /// 缓存自定义类型；构建缓存时的类型名称<br />
        ///     1、用于优化缓存，多个类型公用一个缓存<br />
        /// </summary>
        public Type? Type { init; get; }
    }
}
