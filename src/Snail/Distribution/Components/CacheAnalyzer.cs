using Snail.Aspect.Distribution.Attributes;
using Snail.Aspect.Distribution.Interfaces;

namespace Snail.Distribution.Components;

/// <summary>
/// 缓存分析器
/// </summary>
[Component<ICacheAnalyzer>]
public class CacheAnalyzer : ICacheAnalyzer
{
    #region ICacheAnalyzer
    /// <summary>
    /// 缓存数据主Key
    /// </summary>
    /// <param name="masterKey">缓存主Key；详细参照：<see cref="CacheMethodBase.MasterKey"/></param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    string? ICacheAnalyzer.AnalysisMasterKey(string? masterKey, IDictionary<string, object?>? parameters)
    {
        return ParameterAnalyzer.DEFAULT.Resolve(masterKey, parameters)!;
    }
    /// <summary>
    /// 分析数据key值前缀
    /// </summary>
    /// <param name="dataKeyPrefix">数据key值前缀；详细参照：<see cref="CacheMethodBase.DataKeyPrefix"/></param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    string? ICacheAnalyzer.AnalysisDataKeyPrefix(string? dataKeyPrefix, IDictionary<string, object?>? parameters)
    {
        return ParameterAnalyzer.DEFAULT.Resolve(dataKeyPrefix, parameters)!;
    }
    #endregion
}
