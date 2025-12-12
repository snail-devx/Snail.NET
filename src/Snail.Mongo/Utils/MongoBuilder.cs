using System.Text.RegularExpressions;

namespace Snail.Mongo.Utils;

/// <summary>
/// Mongo数据库构建器
/// <para>1、构建mongo查询条件；强制使用<see cref="BsonDocumentFilterDefinition{T}"/>构建查询条件 </para>
/// <para>- 解决泛型构建时，builder.Eq("propertyName",BsonNull.Value)最终翻译为 {dbField:"BsonNull"}的问题 </para>
/// </summary>
public sealed class MongoBuilder
{
    #region 公共方法

    #region 查询条件构建
    /// <summary>
    /// 恒true
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <remarks>_id始终存在，构建 _id !=null 为恒true条件</remarks>
    /// <returns></returns>
    public static FilterDefinition<DbModel> All<DbModel>() where DbModel : class
        => FilterDefinition<DbModel>.Empty;
    /// <summary>
    /// 恒false
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <returns></returns>
    public static FilterDefinition<DbModel> None<DbModel>() where DbModel : class
        => Eq<DbModel>("_id", null!);

    /// <summary>
    /// 等于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Eq<DbModel>(string field, object? value) where DbModel : class
        => new BsonDocument(field, BsonValue.Create(value));
    /// <summary>
    /// 不等于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Ne<DbModel>(string field, object? value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$ne", BsonValue.Create(value)));

    /// <summary>
    /// 大于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Gt<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$gt", BsonValue.Create(value)));
    /// <summary>
    /// 大于等于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Gte<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$gte", BsonValue.Create(value)));
    /// <summary>
    /// 小于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Lt<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$lt", BsonValue.Create(value)));
    /// <summary>
    /// 小于等于
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Lte<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$lte", BsonValue.Create(value)));

    /// <summary>
    /// In
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> In<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$in", BsonArray.Create(value)));
    /// <summary>
    /// Not In
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Nin<DbModel>(string field, object value) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$nin", BsonArray.Create(value)));

    /// <summary>
    /// Like
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="regex">正则匹配</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Like<DbModel>(string field, Regex regex) where DbModel : class
        => new BsonDocument(field, new BsonRegularExpression(regex));
    /// <summary>
    /// Not Like
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">数据库字段名</param>
    /// <param name="regex">正则匹配</param>
    /// <returns></returns>
    public static FilterDefinition<DbModel> Nlike<DbModel>(string field, Regex regex) where DbModel : class
        => new BsonDocument(field, new BsonDocument("$not", regex));
    #endregion

    #endregion
}
