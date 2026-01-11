using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.Attributes;
/// <summary>
/// 特性标签：数据库缓存
/// <para>1、约束实体需要进行缓存，约束缓存类型等</para>
/// <para>2、配合<see cref="DbTableAttribute" /></para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DbCacheAttribute : Attribute
{
    /// <summary>
    /// 缓存类型：是对象缓存，还是哈希缓存
    /// <para>1、是对象缓存，还是哈希缓存 </para>
    /// <para>2、不传入则默认对象缓存 </para>
    /// </summary>
    public DbCacheType Type { init; get; } = DbCacheType.ObjectCache;

    /// <summary>
    /// 缓存数据Key前缀
    /// <para>1、不传入则直接使用此属性的参数值 </para>
    /// <para>2、传入时，则基于“<see cref="DataKeyPrefix"/>+数据主键id构建缓存Key值 </para>
    /// <para>3、支持动态参数，从方法传入参数值动态构建 </para>
    /// </summary>
    public string? DataKeyPrefix { init; get; }
    /// <summary>
    /// 缓存主Key：根据<see cref="Type"/>取值，此值意义不一样
    /// <para>1、在<see cref="DbCacheType.ObjectCache"/>缓存时，目前忽略 </para>
    /// <para>2、在<see cref="DbCacheType.HashCache"/>缓存时，为Hash缓存key </para>
    /// </summary>
    public string? MasterKey { init; get; }

    /// <summary>
    /// 多少秒后过期
    /// </summary>
    public int? ExpireSeconds { init; get; }

    /// <summary>
    /// 缓存分析器<see cref="IDbCacheAnalyzer"/>的依赖注入Key值
    /// </summary>
    public string? Analyzer { init; get; }
}