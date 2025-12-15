namespace Snail.WebApp.Enumerations;

/// <summary>
/// 枚举：JSON序列化类型
/// </summary>
public enum JsonResolverType
{
    /// <summary>
    /// 保持属性原样输出，不进行大小写转换
    /// </summary>
    Default = 0,

    /// <summary>
    /// 自定义
    /// <para>不满足默认规则的，按此规则处理；然后指定自定义序列化类型</para>
    /// </summary>
    Custom = 1,

    /// <summary>
    /// 驼峰命名：首字母小写
    /// </summary>
    CamelCase = 10,
    /// <summary>
    /// 属性名全小写
    /// </summary>
    LowerCase = 20,
}