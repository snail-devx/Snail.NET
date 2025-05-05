﻿using System.Text.RegularExpressions;
using Snail.Aspect.Web.Interfaces;

namespace Snail.Web.Components
{
    /// <summary>
    /// 默认的<see cref="IHttpAnalyzer"/>请求分析器
    /// </summary>
    [Component<IHttpAnalyzer>(Lifetime = LifetimeType.Singleton)]
    public class HttpAnalyzer : IHttpAnalyzer
    {
        #region 属性方法
        /// <summary>
        /// url参数的正则表达式
        /// </summary>
        private readonly Regex _urlParameterRegex = new Regex("\\{(.+?)\\}", RegexOptions.IgnoreCase);
        #endregion

        #region IHttpAnalyzer：做成虚方法，方便子类继承重写逻辑
        /// <summary>
        /// 分析URL地址
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
        /// <remarks>处理时url参数不区分大小写</remarks>
        /// <returns>处理后的url地址</returns>
        public virtual async Task<string> AnalysisUrl(string url, IDictionary<string, object?>? parameters)
        {
            //  做一个加的异步等待，url为null的情况不可能存在，除非传错了
            if (url == null)
            {
                await Task.Yield();
            }

            return ParameterAnalyzer.DEFAULT.Resolve(url, parameters)!;
        }
        #endregion
    }
}
