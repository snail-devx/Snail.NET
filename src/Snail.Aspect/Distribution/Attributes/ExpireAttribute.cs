using System;

namespace Snail.Aspect.Distribution.Attributes;

/// <summary>
/// 特性标签：过期时间配置
/// <para>1、配合<see cref="CacheMethodAttribute"/>/<see cref="CacheMethodAttribute{T}"/>使用时；指定添加缓存时的过期时间</para>
/// <para>2、配合<see cref="LockMethodAttribute"/>使用时，指定并发锁的过期时间</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ExpireAttribute : Attribute
{
    /// <summary>
    /// 多少秒后过期
    /// </summary>
    public int Seconds { set; get; }
}