using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;

namespace Snail.Elastic.Utils;

/// <summary>
/// ElasticSearch构建器 
/// <para>1、完成查询、聚合相关快捷构建 </para>
/// <para>2、参照Mongo的Builder.Filter快捷方法 </para>
/// </summary>
public static class ElasticBuilder
{
    #region 查询条件构建
    /// <summary>
    /// 恒true；匹配所有数据
    /// </summary>
    /// <returns></returns>
    public static ElasticQueryModel All()
        => new ElasticMatchAllQueryModel();
    /// <summary>
    /// 恒false；匹配空数据
    /// </summary>
    /// <returns></returns>
    public static ElasticQueryModel None()
        => new ElasticMathNoneQueryModel();
    /// <summary>
    /// 字段存在性查询
    /// </summary>
    /// <param name="field"></param>
    /// <param name="exists">true，字段存在；false，字段不存在</param>
    /// <returns></returns>
    public static ElasticQueryModel Exists(string field, bool exists = true)
        => exists ? new ElasticExistsQueryModel(field) : new ElasticExistsQueryModel(field).Not();

    /// <summary>
    /// 等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Eq(string field, string value)
        => new ElasticTermQueryModel(field, value);
    /// <summary>
    /// 等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static ElasticQueryModel Eq(string field, int value)
        => new ElasticTermQueryModel(field, value);
    /// <summary>
    /// 不等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Ne(string field, string value)
        => new ElasticTermQueryModel(field, value).Not();
    /// <summary>
    /// 不等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    public static ElasticQueryModel Ne(string field, int value)
        => new ElasticTermQueryModel(field, value).Not();
    /// <summary>
    /// 大于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Gt(string field, string value)
        => new ElasticRangeQueryModel(field) { GreaterThan = value };
    /// <summary>
    /// 大于等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Gte(string field, string value)
        => new ElasticRangeQueryModel(field) { GreaterEqual = value };
    /// <summary>
    /// 小于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Lt(string field, string value)
        => new ElasticRangeQueryModel(field) { LessThan = value };
    /// <summary>
    /// 小于等于
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Lte(string field, string value)
        => new ElasticRangeQueryModel(field) { LessEqual = value };

    /// <summary>
    /// in
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="values">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel In(string field, List<string> values)
        => new ElasticTermsQueryModel(field, values.ToArray());
    /// <summary>
    /// in
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="values">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel In(string field, string[] values)
        => new ElasticTermsQueryModel(field, values);
    /// <summary>
    /// not in
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="values">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Nin(string field, List<string> values)
       => new ElasticTermsQueryModel(field, values.ToArray()).Not();
    /// <summary>
    /// not in
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="values">字段值；外部做好null判断处理</param>
    /// <returns></returns>
    public static ElasticQueryModel Nin(string field, string[] values)
        => new ElasticTermsQueryModel(field, values).Not();

    /// <summary>
    /// like
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理；做好正则关键字转义</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public static ElasticQueryModel Like(string field, string value, bool ignoreCase)
        => new ElasticWildcardQueryModel(field, value, ignoreCase);
    /// <summary>
    /// not like
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值；外部做好null判断处理；做好正则关键字转义</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public static ElasticQueryModel Nlike(string field, string value, bool ignoreCase)
        => new ElasticWildcardQueryModel(field, value, ignoreCase).Not();
    #endregion

    #region 聚合操作构建

    #endregion
}
