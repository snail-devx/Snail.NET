using System.Text.RegularExpressions;

namespace Snail.Abstractions.Setting.Extensions;

/// <summary>
/// <see cref="ISettingManager"/> 相关扩展
/// </summary>
public static partial class SettingManagerExtensions
{
    #region 属性变量
    /// <summary>
    /// 环境变量 正则表达式
    /// <para>1、使用<see cref="GeneratedRegexAttribute"/>在生成节点编译好，避免运行时编译和JIT，从而优化性能</para>
    /// </summary>
    [GeneratedRegex(@"\$\{(.+?[^\\])\}")]
    private static partial Regex REGEX_EnvVar { get; }
    #endregion

    #region 扩展方法
    extension(ISettingManager manager)
    {
        /// <summary>
        /// 解析输入字符串中的环境变量
        /// <para>1、将环境变量，采用具体的值替换，内部使用<see cref="ISettingManager.GetEnv(in string)"/>取环境变量值</para>
        /// <para>2、环境变量格式“${环境变量名称}”；如“my name is ${user}”，会将"${user}"替换成 "user" 环境变量值</para>
        /// <para>3、环境变量名称，区分大小写；并确保存在，否则解析时会报错；</para>
        /// </summary>
        /// <param name="input"></param>
        /// <exception cref="ApplicationException">环境变量不存在时</exception>
        /// <returns>解析后的字符串</returns>
        public string AnalysisEnvVars(in string input)
        {
            if (IsNullOrEmpty(input) == false)
            {
                return REGEX_EnvVar.Replace(input, match =>
                {
                    string name = match.Groups[1].Value;
                    string? value = manager.GetEnv(name);
                    if (value == null)
                    {
                        string message = $"变量[{name}]无法从环境变量中查询到具体值。环境变量：{match.Groups[0].Value}";
                        throw new ApplicationException(message);
                    }
                    return value!;
                });
            }
            return input;
        }
    }
    #endregion
}
