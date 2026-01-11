using Newtonsoft.Json;
using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;

namespace Snail.Abstractions.Database.Interfaces;
/// <summary>
/// 接口约束：数据库实体代理
/// </summary>
public interface IDbModelProxy
{
    /// <summary>
    /// 数据库实体表信息
    /// </summary>
    DbModelTable Table { get; }
    /// <summary>
    /// 数据库实体表名
    /// </summary>
    string TableName => Table.Name;
    /// <summary>
    /// 主键字段信息
    /// </summary>
    DbModelField PKField => Table.PKField;
    /// <summary>
    /// 数据库实体字段映射
    /// <para>1、Key为C#中的属性名称，Value为实体字段相关信息</para>
    /// </summary>
    IReadOnlyDictionary<string, DbModelField> FieldMap { get; }
    /// <summary>
    /// 数据库实体对应的Json序列化设置
    /// </summary>
    JsonSerializerSettings JsonSetting { get; }

    /// <summary>
    /// 数据库缓存标签
    /// </summary>
    DbCacheAttribute? CacheOptions { get; }
}