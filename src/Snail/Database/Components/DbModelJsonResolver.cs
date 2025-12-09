using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;
using Snail.Database.Utils;

namespace Snail.Database.Components;

/// <summary>
/// 数据库实体JSON数据解析器 <br />
///     1、进行JSON序列化和反序列化时，把DbModel的属性名映射为<see cref="DbFieldAttribute.Name"/>值 <br />
///     2、剔除掉<see cref="DbFieldAttribute.Ignored"/>为true的属性映射 <br />
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public sealed class DbModelJsonResolver<DbModel> : DefaultContractResolver where DbModel : class
{
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
        if (typeof(DbModel) == type)
        {
            DbModelTable table = DbModelHelper.GetTable(type);
            foreach (var pi in pis)
            {
                //  取fieldmap中的数据，取不到则强制标记为ignore
                DbModelField? field = table.Fields.FirstOrDefault(field =>
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
