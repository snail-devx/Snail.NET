using Snail.Aspect.Web.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snail.Aspect.Web.Interfaces;

/// <summary>
/// 接口约束：Http请求分析器
/// <para>1、处理http请求url上的动态参数信息 </para>
/// <para>2、配合<see cref="HttpAspectAttribute"/>在进行自动生成HTTP请求接口实现类时，可干预url、body、header等参数值 </para>
/// </summary>
public interface IHttpAnalyzer
{
    /// <summary>
    /// 分析URL地址
    /// </summary>
    /// <param name="url">请求url地址</param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    /// <remarks>处理时url参数不区分大小写</remarks>
    /// <returns>处理后的url地址</returns>
    Task<string> AnalysisUrl(string url, IDictionary<string, object?>? parameters);
}
