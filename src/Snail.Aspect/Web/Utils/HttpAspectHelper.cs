using Snail.Aspect.Web.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snail.Aspect.Web.Utils;

/// <summary>
/// HTTP切面编程助手类；用于运行时辅助执行HTTP操作
/// </summary>
public static class HttpAspectHelper
{
    #region 公共方法
    /// <summary>
    /// 分析HTTP请求的Url地址，根据需要执行解析器
    /// </summary>
    /// <param name="analyzer"></param>
    /// <param name="url"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async static Task<string> AnalysisHttpUrl(IHttpAnalyzer analyzer, string url, IDictionary<string, object?>? parameters)
    {
        if (analyzer != null)
        {
            url = await analyzer.AnalysisUrl(url, parameters);
            if (string.IsNullOrEmpty(url) == true)
            {
                throw new ArgumentNullException("AnalysisUrl后，url地址为null");
            }
        }
        return url;
    }
    #endregion
}

