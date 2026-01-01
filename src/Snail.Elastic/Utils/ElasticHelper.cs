using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Web.DataModels;
using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;
using Snail.Web.Components;
using System.Diagnostics;
using System.Text;

namespace Snail.Elastic.Utils;

/// <summary>
/// ElasticSearch助手类
/// </summary>
public static class ElasticHelper
{
    #region 属性变量
    /// <summary>
    /// URL参数：仅需要Source字段值
    /// </summary>
    public const string PARAM_OnlySource = "filter_path=hits.hits._source";
    /// <summary>
    /// 最大数据量
    /// </summary>
    public const int MAX_Size = 10000;
    #endregion

    #region 公共方法

    #region 查询条件快捷构建
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

    #region 聚合操作快捷构建

    #endregion

    #region 和服务器打交道
    /// <summary>
    /// 构建Elastic操作
    /// </summary>
    /// <param name="tableName">表名：es索引名；若不指定则全库操作，有安全隐患；会强制小写</param>
    /// <param name="action">要执行的操作名；不可为空；</param>
    /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
    /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
    /// <returns>构建好的操作api地址</returns>
    public static string BuildAction(in string tableName, in string action, in string? routing, IList<string>? urlParams = null)
    {
        //  action强制不能为空
        ThrowIfNullOrEmpty(action);
        if (routing?.Length > 0)
        {
            urlParams ??= [];
            urlParams.Insert(0, $"routing={routing}");
        }
        return tableName?.Length > 0
            ? $"/{tableName.ToLower()}/{action}?{urlParams?.AsString('&')}"
            : $"/{action}?{urlParams?.AsString('&')}";
    }

    //  后期在这里做一些其他通用逻辑，单纯不带日志记录的；如通用的Search、Insert、Save、Bulk、、、

    /// <summary>
    /// 执行Post发送字符串数据；内部会记录post和response数据
    /// </summary>
    /// <param name="title">操作标题，用作日志标题</param>
    /// <param name="dbServer">数据库服务器</param>
    /// <param name="api">api地址</param>
    /// <param name="post">post数据</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="logPost">是否记录post数据；若post数据量特别大，则考虑false；否则影响性能</param>
    /// <param name="logResult">是否记录结果日志；若结果数据量特别大，则考虑false；否则影响性能</param>
    /// <returns>post请求结果</returns>
    public static async Task<string?> PostString(string title, DbServerDescriptor dbServer, string api, string post, ILogger logger, bool logPost = true, bool logResult = true)
    {
        string? ret = null;
        Stopwatch sw = Stopwatch.StartNew();
        Exception? tmpEx = null;
        try
        {
            Uri baseAddress = new(dbServer.Connection);
            HttpRequestMessage request = new(HttpMethod.Post, api)
            {
                Content = new StringContent(post, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await HttpProxy.Send(baseAddress, request);
            HttpResult hr = new(response);
            ret = await hr.AsStringAsync;
            return ret;
        }
        catch (Exception ex)
        {
            tmpEx = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            post = logPost ? post : "调用方约束不记录post数据";
            ret = logResult ? ret : "调用方约束不记录response结果";
            string logContent = $"api:{api}; performance:{sw.ElapsedMilliseconds}; post:{post}; ret:{ret}";
            var _ = tmpEx == null
                ? logger.Trace(title, logContent)
                : logger.Error(title, logContent, tmpEx);
        }
    }
    #endregion

    #endregion
}
