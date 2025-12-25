using Snail.Aspect.Distribution.Enumerations;
using System;

namespace Snail.Aspect.Distribution.Attributes;

/// <summary>
/// 缓存方法基类
/// <para>抽取通用信息，方便派生类使用</para>
/// <para>派生类型：<see cref="CacheMethodAttribute"/>和<see cref="CacheMethodAttribute{T}"/></para>
/// </summary>
public abstract class CacheMethodBase : Attribute
{
    /// <summary>
    /// 缓存类型：是对象缓存，还是哈希缓存
    /// <para>1、是对象缓存，还是哈希缓存 </para>
    /// <para>2、不传入则默认对象缓存 </para>
    /// </summary>
    public CacheType Type { set; get; } = CacheType.ObjectCache;
    /// <summary>
    /// 缓存操作类型
    /// <para>1、<see cref="CacheActionType.Load"/>：传入缓存key，取到了则直接返回；取不到再执行方法代码取新数据；若传入多key，则自动过滤取到的，最后再合并 </para>
    /// <para>2、<see cref="CacheActionType.LoadSave"/>：传入缓存key，取到了则直接返回；取不到再执行方法代码取新数据并加入缓存；若传入多key，则自动过滤取到的 </para>
    /// <para>3、<see cref="CacheActionType.Save"/>：方法返回数据，自动加入缓存中 </para>
    /// <para>4、<see cref="CacheActionType.Delete"/>：传入的缓存Key，执行完方法后自动删除 </para>
    /// <para>5、不传入则默认Load </para>
    /// </summary>
    public CacheActionType Action { set; get; } = CacheActionType.Load;

    /// <summary>
    /// 缓存数据Key前缀
    /// <para>1、不传入则直接使用此属性的参数值 </para>
    /// <para>2、传入时，则基于“<see cref="DataKeyPrefix"/>+<see cref="CacheKeyAttribute"/>标记参数值”；若方法参数为批量数组，则循环每个元素做构建 </para>
    /// <para>3、支持动态参数，从方法传入参数值动态构建 </para>
    /// </summary>
    public string? DataKeyPrefix { get; set; }
    /// <summary>
    /// 缓存主Key：根据<see cref="Type"/>取值，此值意义不一样
    /// <para>1、在<see cref="CacheType.ObjectCache"/>缓存时，目前忽略 </para>
    /// <para>2、在<see cref="CacheType.HashCache"/>缓存时，为Hash缓存key </para>
    /// </summary>
    public string? MasterKey { set; get; }
}

/// <summary>
/// 属性标签：缓存方法，标记此方法要进行缓存操作
/// <para>1、配合<see cref="CacheKeyAttribute"/>使用，可指定缓存数据Key </para>
/// <para>2、配合<see cref="ExpireAttribute"/>使用，可在保存缓存时，指定缓存过期时间</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CacheMethodAttribute : CacheMethodBase
{
    /// <summary>
    /// 缓存数据类型；
    /// <para>1、必传，基于此分析缓存数据类型 </para>
    /// <para>2、最初想基于方法返回值分析，这样限制太多，且分析得不一定准确 </para>
    /// </summary>
    public required Type DataType { set; get; }
}

/// <summary>
/// 属性标签：缓存方法，标记此方法要进行缓存操作
/// <para>1、配合<see cref="CacheKeyAttribute"/>使用，可指定缓存数据Key </para>
/// <para>2、配合<see cref="ExpireAttribute"/>使用，可在保存缓存时，指定缓存过期时间</para>
/// </summary>
/// <typeparam name="T">缓存数据类型；用其重写<see cref="CacheMethodAttribute.DataType"/>值</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CacheMethodAttribute<T> : CacheMethodBase
{
}