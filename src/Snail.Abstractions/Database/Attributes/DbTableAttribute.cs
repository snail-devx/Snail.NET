using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.Attributes;

/// <summary>
/// 特性标签：数据库表属性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DbTableAttribute : Attribute
{
    /// <summary>
    /// 数据表名称；为空时使用实体名
    /// </summary>
    /// <remarks>配合内部整理</remarks>
    public string? Name { get; init; }

    ///// <summary>
    ///// 数据表字段映射时，是否考虑继承的属性
    ///// <para>暂时不对外开放，始终考虑继承的属性 </para>
    ///// </summary>
    //public bool Inherited { get; init; } = true;

    /// <summary>
    /// 是否启用数据路由分片存储
    /// <para>1、为true时，实体必须实现<see cref="IDbRouting.GetRouting"/>接口方法，且值不能为空 </para>
    /// <para>2、具体能否实现分片存储，还得看数据库和具体<see cref="IDbProvider"/>实现类是否是否支持 </para>
    /// </summary>
    public bool Routing { init; get; }
}
