using Snail.Aspect.Web.Interfaces;
using Snail.Web.Components;


namespace Snail.Test.CodeAnalysis.Components
{
    /// <summary>
    /// 
    /// </summary>
    [Component<IHttpAnalyzer>(Key = "TestAnalyzer")]
    public sealed class TestAnalyzer : HttpAnalyzer, IHttpAnalyzer
    {
        #region IHttpAnalyzer
        /// <summary>
        /// 分析URL地址
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
        /// <remarks>处理时url参数不区分大小写</remarks>
        /// <returns>处理后的url地址</returns>
        Task<string> IHttpAnalyzer.AnalysisUrl(string url, IDictionary<string, object?>? parameters)
        {
            return AnalysisUrl(url, parameters);
        }
        #endregion
    }
}
