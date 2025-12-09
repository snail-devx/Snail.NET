using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Web.DataModels;
using Snail.Utilities.Collections.Extensions;
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
            urlParams ??= new List<string>();
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
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, api)
            {
                Content = new StringContent(post, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await HttpProvider.Send(baseAddress, request);
            HttpResult hr = new HttpResult(response);
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
}
