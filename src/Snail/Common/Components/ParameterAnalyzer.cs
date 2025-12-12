using System.Text.RegularExpressions;

namespace Snail.Common.Components;

/// <summary>
/// 参数分析器
/// <para>1、分析字符串中的动态参数，进行参数替换；从而生成实际字符串数据  </para>
/// <para>2、如 {name} 将被识别为name参数；然后从传入的name参数值替换字符串中的{name} </para>
/// <para>3、支持外部指定 参数识别 规则，默认为 {parameter} </para>
/// </summary>
public class ParameterAnalyzer
{
    #region 属性变量
    /// <summary>
    /// 默认的参数解析器
    /// </summary>
    public static readonly ParameterAnalyzer DEFAULT = new();

    /// <summary>
    /// 参数匹配规则
    /// </summary>
    public Regex Rule { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法：采用默认参数匹配规则 {name}
    /// </summary>
    public ParameterAnalyzer()
    {
        Rule = new Regex("\\{(.+?)\\}", RegexOptions.IgnoreCase);
    }
    /// <summary>
    /// 构造方法：可指定参数匹配规则
    /// </summary>
    /// <param name="rule"></param>
    public ParameterAnalyzer(Regex rule)
    {
        Rule = ThrowIfNull(rule);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 分析参数：获取<paramref name="str"/>中的参数信息
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public IList<string>? Analysis(string str)
    {
        return string.IsNullOrEmpty(str) == false
            ? Rule.Matches(str).Select(match => match.Groups[1].Value).Distinct().ToList()
            : null;
    }
    /// <summary>
    /// 解析参数：将<paramref name="str"/>中的参数使用<paramref name="parameters"/>中值替换
    /// </summary>
    /// <param name="str"></param>
    /// <param name="parameters">参数字典；key为参数名，value为参数值</param>
    /// <returns></returns>
    public string? Resolve(string? str, IDictionary<string, object?>? parameters)
    {
        if (string.IsNullOrEmpty(str) == false)
        {
            str = Rule.Replace(str, match =>
            {
                string name = match.Groups[1].Value;
                string? value = parameters
                    ?.FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value
                    ?.ToString();
                if (value == null)
                {
                    string message = $"参数[{name}]无法从parameters中查询到参数值。str:{str}；parameters:{parameters.AsJson()}";
                    throw new ArgumentException(message);
                }
                return value;
            });
        }
        return str;
    }
    #endregion
}
