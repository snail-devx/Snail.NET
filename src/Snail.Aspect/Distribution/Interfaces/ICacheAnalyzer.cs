using System.Collections.Generic;

namespace Snail.Aspect.Distribution.Interfaces;

/// <summary>
/// 接口：缓存分析器
/// <para>1、前置分析：分析缓存key和masterkey值 </para>
/// <para>2、后置分析：分析缓存有效性、、、，暂不支持 </para>
/// </summary>
public interface ICacheAnalyzer
{
    /// <summary>
    /// 缓存数据主Key
    /// </summary>
    /// <param name="masterKey">缓存主Key；详细参照：<see cref="Attributes.CacheMethodBase.MasterKey"/></param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    string? AnalysisMasterKey(string? masterKey, IDictionary<string, object?>? parameters);
    /// <summary>
    /// 分析数据key值前缀
    /// </summary>
    /// <param name="dataKeyPrefix">数据key值前缀；详细参照：<see cref="Attributes.CacheMethodBase.DataKeyPrefix"/></param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    string? AnalysisDataKeyPrefix(string? dataKeyPrefix, IDictionary<string, object?>? parameters);
}
