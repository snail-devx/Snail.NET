using System;

namespace Snail.Aspect.Distribution.Attributes;

/// <summary>
/// 属性标签：缓存Key，标记此参数是缓存Key值 <br />
///     1、配合<see cref="CacheMethodAttribute"/>使用 <br />
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CacheKeyAttribute : Attribute
{
}
