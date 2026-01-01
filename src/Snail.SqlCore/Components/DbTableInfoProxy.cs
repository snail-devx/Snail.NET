using Snail.Abstractions.Database.Enumerations;
using Snail.Database.Components;
using Snail.SqlCore.Interfaces;
using Snail.Utilities.Collections;
using Snail.Utilities.Collections.Extensions;
using System.Collections.ObjectModel;

namespace Snail.SqlCore.Components;
/// <summary>
/// 数据表信息代理
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public sealed class DbTableInfoProxy<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 代理字典；key为数据库类型，value为代理对象
    /// </summary>
    private static readonly LockMap<DbType, DbTableInfoProxy<DbModel>> _proxyMap = new();

    /// <summary>
    /// 数据表类型；已进行关键字处理
    /// </summary>
    public required string DbTableName { init; get; }
    /// <summary>
    /// 默认的Insert语句
    /// <para>1、拼接<typeparamref name="DbModel"/>所有属性字段，并进行关键字处理</para>
    /// </summary>
    public required string InsertSql { init; get; }
    /// <summary>
    /// 默认的Select语句
    /// <para>1、拼接<typeparamref name="DbModel"/>所有属性字段，并进行关键字处理</para>
    /// </summary>
    public required string SelectSql { init; get; }
    /// <summary>
    /// 数据库字段名称映射
    /// <para>1、Key为属性名称，Value为对应的数据库字段名</para>
    /// <para>2、数据库字段名，已进行关键字处理</para>
    /// </summary>
    public required IReadOnlyDictionary<string, string> DbFieldNameMap { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    private DbTableInfoProxy()
    { }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取代理对象
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static DbTableInfoProxy<DbModel> GetProxy(ISqlProvider provider)
    {
        return _proxyMap.GetOrAdd(provider.DbType, (_) =>
        {
            DbModelProxy proxy = DbModelProxy.GetProxy<DbModel>();
            string dbTableName = $"{provider.KeywordLeftToken}{proxy.TableName}{provider.KeywordRightToken}";
            List<string> dbFields = [], paramNames = [], selectFields = [];
            Dictionary<string, string> fieldMap = [];
            foreach (var field in proxy.FieldMap.Values)
            {
                string fieldName = $"{provider.KeywordLeftToken}{field.Name}{provider.KeywordRightToken}";
                fieldMap[field.Property.Name] = fieldName;
                //  临时集合处理，方便后续组装固定sql语句
                dbFields.Add(fieldName);
                paramNames.Add($"{provider.ParameterToken}{field.Property.Name}");
                //  组装select查询字段信息；若和属性名称不一致，则需要做一下as操作
                if (field.Name != field.Property.Name)
                {
                    fieldName = $"{fieldName} AS {provider.KeywordLeftToken}{field.Property.Name}{provider.KeywordRightToken}";
                }
                selectFields.Add(fieldName);
            }
            var dbFieldNameMap = new ReadOnlyDictionary<string, string>(fieldMap);
            //  缓存字段映射；数据插入sql语句；全字段select语句
            string insertSql = $"INSERT INTO {dbTableName} ({dbFields.AsString(", ")}) VALUES({paramNames.AsString(", ")})";
            string selectSql = $"SELECT {selectFields.AsString(", ")} FROM {dbTableName}";

            return new DbTableInfoProxy<DbModel>()
            {
                DbTableName = dbTableName,
                InsertSql = insertSql,
                SelectSql = selectSql,
                DbFieldNameMap = dbFieldNameMap
            };
        });
    }

    /// <summary>
    /// 获取数据库字段名称，已进行关键字处理
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public string GetDbFieldName(string propertyName, string title)
    {
        if (DbFieldNameMap.TryGetValue(propertyName, out string? dbFieldName) == false)
        {
            string msg = $"{title}：无法查找成员{propertyName}对应的数据库字段名称。DbModel：{typeof(DbModel)}";
            throw new KeyNotFoundException(msg);
        }
        return dbFieldName;
    }
    #endregion
}