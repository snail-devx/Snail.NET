using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;

namespace Snail.Database.Components;

/// <summary>
/// 数据库实体JSON数据解析器 
/// <para>1、进行JSON序列化和反序列化时，把DbModel的属性名映射为<see cref="DbFieldAttribute.Name"/>值 </para>
/// <para>2、剔除掉<see cref="DbFieldAttribute.Ignored"/>为true的属性映射 </para>
/// </summary>
public sealed class DbModelJsonResolver : DefaultContractResolver
{
    #region 属性字段
    /// <summary>
    /// 表信息
    /// </summary>
    private readonly DbModelTable _table;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="table"></param>
    public DbModelJsonResolver(DbModelTable table)
    {
        _table = ThrowIfNull(table);
    }
    #endregion

    #region 重写方法
    /// <summary>
    /// 创建指定类型的字段属性映射
    /// </summary>
    /// <param name="type"></param>
    /// <param name="memberSerialization"></param>
    /// <returns></returns>
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        /*
         * 基本思路：正常构建，当Type是DbModel时，做剔除和重命名PropertyName处理
         */
        IList<JsonProperty> pis = base.CreateProperties(type, memberSerialization);
        //  遍历DbModel类型构建的pis，剔除ignore标记属性，并进行FieldName映射
        if (_table.Type == type)
        {
            foreach (var pi in pis)
            {
                //  取fieldmap中的数据，取不到则强制标记为ignore
                DbModelField? field = _table.Fields.FirstOrDefault(field =>
                    field.Property.Name == pi.PropertyName &&
                    field.Property.PropertyType == pi.PropertyType &&
                    field.Property.DeclaringType == pi.DeclaringType
                );
                pi.Ignored = field == null;
                pi.PropertyName = field?.Name ?? pi.PropertyName;
            }
        }
        return pis;
    }
    #endregion
}
