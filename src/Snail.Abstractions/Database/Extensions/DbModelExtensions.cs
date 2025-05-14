using System.Collections.ObjectModel;
using Snail.Abstractions.Database.DataModels;

namespace Snail.Abstractions.Database.Extensions
{
    /// <summary>
    /// 数据库实体扩展方法
    /// </summary>
    public static class DbModelExtensions
    {
        #region 公共方法

        #region DbModelTable
        /// <summary>
        /// 获取字段信息字典
        /// </summary>
        /// <param name="table"></param>
        /// <returns>key为实体属性名称，value为对应的字段信息</returns>
        public static IReadOnlyDictionary<string, DbModelField> GetFieldMap(this DbModelTable table)
        {
            Dictionary<string, DbModelField> map = new Dictionary<string, DbModelField>();
            foreach (var field in table.Fields)
            {
                map[field.Property.Name] = field;
            }
            return new ReadOnlyDictionary<string, DbModelField>(map);
        }

        /// <summary>
        /// 基于实体属性名获取字段信息
        /// </summary>
        /// <param name="table"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static DbModelField? GetField(this DbModelTable table, string propertyName)
        {
            ThrowIfNullOrEmpty([propertyName]);
            return table.Fields.FirstOrDefault(field => field.Property.Name == propertyName);
        }
        #endregion

        #endregion
    }
}
